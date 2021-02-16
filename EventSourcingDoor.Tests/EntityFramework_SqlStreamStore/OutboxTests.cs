using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Outboxes;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SqlStreamStore;
using SqlStreamStore.Streams;

namespace EventSourcingDoor.Tests.EntityFramework_SqlStreamStore
{
    public class OutboxTests : OutboxTestsBase
    {
        private static Randomizer Random => TestContext.CurrentContext.Random;
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IOutbox _outbox;
        private IStreamStore _eventStore;

        [SetUp]
        public async Task EnsureSchemaInitialized()
        {
            var eventStore = new MsSqlStreamStoreV3(new MsSqlStreamStoreV3Settings(ConnectionString));
            await eventStore.DropAll();
            await eventStore.CreateSchemaIfNotExists();
            _eventStore = eventStore;
            _outbox = new SqlStreamStoreOutbox(eventStore);
            var db = new TestDbContextWithOutbox(ConnectionString, _outbox);
            db.Database.CreateIfNotExists();
        }

        [Test]
        public async Task Catchup_subscription_should_not_drop_messages()
        {
            // Given
            var events = new List<IDomainEvent>();
            // `guaranteedDelay` should include transaction timeout + clock drift. Otherwise, `PollingClient2` may skip commits.
            var guaranteedDelay = TimeSpan.FromMilliseconds(3000);
            new OutboxAwareStreamStore(_eventStore, guaranteedDelay).SubscribeToAll(null, ReceiveEvent);
            await Task.Delay(1000);

            async Task ReceiveEvent(IAllStreamSubscription _, StreamMessage message, CancellationToken __)
            {
                var data = await message.GetJsonData();
                Console.WriteLine(message.Position + ": " + data);
                var evt = (IDomainEvent) JsonConvert.DeserializeObject(data, Type.GetType(message.Type));
                lock (events)
                    events.Add(evt);
            }


            var user1 = new UserAggregate(Random.NextGuid(), "User#1");
            var user2 = new UserAggregate(Random.NextGuid(), "User#2");
            var longTransaction = new TaskCompletionSource<object>();

            // Warm-up
            await InsertUserInTransaction(new UserAggregate(Random.NextGuid(), Random.GetString()), Task.CompletedTask);

            // When
            var longTask = InsertUserInTransaction(user1, longTransaction.Task);
            await Task.Delay(1000);
            var shortTask = InsertUserInTransaction(user2, Task.CompletedTask);
            await Task.Delay(1000);
            await shortTask;
            longTransaction.SetResult(null);
            await longTask;

            // Then
            await Task.Delay(5000);
            lock (events)
            {
                events.OfType<UserRegistered>().Should().Contain(e => e.Id == user1.Id);
                events.OfType<UserRegistered>().Should().Contain(e => e.Id == user2.Id);
            }
        }

        protected override TestDbContextWithOutbox NewDbContext()
        {
            return new TestDbContextWithOutbox(ConnectionString, _outbox);
        }

        protected override async Task<List<IDomainEvent>> LoadChangeLog(string streamId)
        {
            var changeLog = new List<IDomainEvent>();
            var page = await _eventStore.ReadStreamForwards(streamId, 0, int.MaxValue);
            foreach (var message in page.Messages)
            {
                var json = await message.GetJsonData();
                var evt = (IDomainEvent) JsonConvert.DeserializeObject(json, Type.GetType(message.Type));
                changeLog.Add(evt);
            }

            return changeLog;
        }

        protected override async Task<List<IDomainEvent>> LoadAllChangeLogs()
        {
            var changeLog = new List<IDomainEvent>();
            var page = await _eventStore.ReadAllForwards(0, int.MaxValue);
            foreach (var message in page.Messages)
            {
                var json = await message.GetJsonData();
                var evt = (IDomainEvent) JsonConvert.DeserializeObject(json, Type.GetType(message.Type));
                changeLog.Add(evt);
            }

            return changeLog;
        }
    }
}
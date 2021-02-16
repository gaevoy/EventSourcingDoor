using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Outboxes;
using FluentAssertions;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.PollingClient;
using NEventStore.Serialization.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;

#pragma warning disable 1998

namespace EventSourcingDoor.Tests.EntityFramework_NEventStore_PostgreSql
{
    [Parallelizable(ParallelScope.None)]
    public class OutboxTests: OutboxTestsBase
    {
        private static Randomizer Random => TestContext.CurrentContext.Random;
        public string ConnectionString => "EventSourcingDoorConnectionString";
        private IOutbox _outbox;
        private IStoreEvents _eventStore;

        [SetUp]
        public async Task EnsureSchemaInitialized()
        {
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(ConnectionString)
                .WithDialect(new PostgreSqlDialect())
                .InitializeStorageEngine()
                .UsingJsonSerialization()
                .Build();
            eventStore.Advanced.Purge();
            _eventStore = eventStore;
            _outbox = new NEventStoreOutbox(eventStore);
            var db = new TestDbContextWithOutbox(ConnectionString, _outbox);
            db.Database.CreateIfNotExists();
            // Warm-up
            for (int i = 0; i < 2; i++)
            {
                using var warmUpDb = NewDbContext();
                warmUpDb.Users.Add(new UserAggregate(Guid.NewGuid(), ""));
                await warmUpDb.SaveChangesAsync();
            }
        }

        [Test]
        public async Task Catchup_subscription_should_not_drop_messages()
        {
            // Given
            var events = new List<IDomainEvent>();
            // `guaranteedDelay` should include transaction timeout + clock drift. Otherwise, `PollingClient2` may skip commits.
            var guaranteedDelay = TimeSpan.FromMilliseconds(3000);
            var pollingClient = new PollingClient2(_eventStore.Advanced, commit =>
            {
                var visibilityDate = DateTime.UtcNow - guaranteedDelay;
                if (commit.CommitStamp > visibilityDate)
                    return PollingClient2.HandlingResult.Retry; // Wait more for the guaranteed delay
                lock (events)
                    events.AddRange(commit.Events.Select(e => e.Body).OfType<IDomainEvent>());
                return PollingClient2.HandlingResult.MoveToNext;
            });
            pollingClient.StartFrom();
            await Task.Delay(1000);

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

        protected override EventSourcingDoor.Tests.Domain.TestDbContextWithOutbox NewDbContext()
        {
            return new TestDbContextWithOutbox(ConnectionString, _outbox);
        }

        protected override async Task<List<IDomainEvent>> LoadChangeLog(string streamId)
        {
            return _eventStore.Advanced.GetFrom(Bucket.Default, streamId, 0, int.MaxValue)
                .SelectMany(e => e.Events)
                .Select(e => e.Body)
                .OfType<IDomainEvent>()
                .ToList();
        }

        protected override async Task<List<IDomainEvent>> LoadAllChangeLogs()
        {
            return _eventStore.Advanced.GetFrom(0)
                .SelectMany(e => e.Events)
                .Select(e => e.Body)
                .OfType<IDomainEvent>()
                .ToList();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Utils;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SqlStreamStore;
using SqlStreamStore.Streams;

namespace EventSourcingDoor.Tests.SqlStreamStoreUsage
{
    public class EventSourcedEntityTests
    {
        private static Randomizer Random => TestContext.CurrentContext.Random;
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IStreamStore _streamStore;

        [SetUp]
        public async Task EnsureSchemaInitialized()
        {
            var streamStore = new MsSqlStreamStoreV3(new MsSqlStreamStoreV3Settings(ConnectionString));
            _streamStore = streamStore;
            var db = new Db(ConnectionString, _streamStore);
            await streamStore.DropAll();
            await streamStore.CreateSchemaIfNotExists();
            db.Database.CreateIfNotExists();
        }

        [Test]
        public async Task It_should_insert_entity_and_record_change_log()
        {
            // Given
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var changeLog = user.GetUncommittedChanges().ToList();

            // When
            await InsertUser(user);

            // Then
            var savedUser = await LoadUser(user.Id);
            var savedChangeLog = await LoadChangeLog(user.StreamId);
            savedUser.Should().BeEquivalentTo(user);
            for (var i = 0; i < savedChangeLog.Count; i++)
            {
                savedChangeLog[i].Should().BeEquivalentTo((object) changeLog[i]);
                savedChangeLog[i].Should().BeOfType(changeLog[i].GetType());
            }
        }

        [Test]
        public async Task It_should_update_entity_and_record_change_log()
        {
            // Given
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            await InsertUser(user);
            user = await LoadUser(user.Id);

            // When
            user.Rename("James Bond");
            var changeLog = user.GetUncommittedChanges().ToList();
            await UpdateUser(user);

            // Then
            var savedUser = await LoadUser(user.Id);
            var savedChangeLog = (await LoadChangeLog(user.StreamId)).AsEnumerable()
                .Reverse()
                .Take(changeLog.Count)
                .Reverse()
                .ToList();
            savedUser.Should().BeEquivalentTo(user);
            for (var i = 0; i < savedChangeLog.Count; i++)
            {
                savedChangeLog[i].Should().BeEquivalentTo((object) changeLog[i]);
                savedChangeLog[i].Should().BeOfType(changeLog[i].GetType());
            }
        }

        [Test]
        public async Task Long_transaction_should_not_block_short_transactions()
        {
            // Warm-up
            await InsertUserInTransaction(new UserAggregate(Random.NextGuid(), Random.GetString()), Task.CompletedTask);

            // Given
            var user1 = new UserAggregate(Random.NextGuid(), "User#1");
            var user2 = new UserAggregate(Random.NextGuid(), "User#2");
            var longTransaction = new TaskCompletionSource<object>();

            // When
            var longTask = InsertUserInTransaction(user1, longTransaction.Task);
            await Task.Delay(500);
            var shortTask = InsertUserInTransaction(user2, Task.CompletedTask);
            await Task.Delay(500);
            var logsAfterShortTask = await LoadAllChangeLogs();
            var longTaskStatus = longTask.Status;
            var shortTaskStatus = shortTask.Status;
            longTransaction.SetResult(null);
            await Task.WhenAll(longTask, shortTask);
            var logs = await LoadAllChangeLogs();

            // Then
            longTaskStatus.Should().Be(TaskStatus.WaitingForActivation);
            longTask.Status.Should().Be(TaskStatus.RanToCompletion);
            shortTaskStatus.Should().Be(TaskStatus.RanToCompletion);
            shortTask.Status.Should().Be(TaskStatus.RanToCompletion);
            logsAfterShortTask.OfType<UserRegistered>().Should().NotContain(e => e.Id == user1.Id);
            logsAfterShortTask.OfType<UserRegistered>().Should().Contain(e => e.Id == user2.Id);
            logs.OfType<UserRegistered>().Should().Contain(e => e.Id == user1.Id);
            logs.OfType<UserRegistered>().Should().Contain(e => e.Id == user2.Id);
        }

        [Test]
        public async Task Catchup_subscription_should_not_drop_messages()
        {
            // Given
            var events = new List<IDomainEvent>();
            _streamStore.SubscribeToAll(null, ReceiveEvent);
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

        private async Task InsertUser(UserAggregate user)
        {
            using var db = new Db(ConnectionString, _streamStore);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        private async Task UpdateUser(UserAggregate user)
        {
            using var db = new Db(ConnectionString, _streamStore);
            db.Entry(user).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        private async Task<UserAggregate> LoadUser(Guid id)
        {
            using var db = new Db(ConnectionString, _streamStore);
            return await db.Users.FindAsync(id);
        }

        private async Task InsertUserInTransaction(UserAggregate user, Task beforeTransactionDone)
        {
            await Task.Yield();
            using var transaction = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted);
            await InsertUser(user);
            await beforeTransactionDone;
            transaction.Complete();
        }

        private async Task<List<IDomainEvent>> LoadChangeLog(string streamId)
        {
            var changeLog = new List<IDomainEvent>();
            var page = await _streamStore.ReadStreamForwards(streamId, 0, int.MaxValue);
            foreach (var message in page.Messages)
            {
                var json = await message.GetJsonData();
                var evt = (IDomainEvent) JsonConvert.DeserializeObject(json, Type.GetType(message.Type));
                changeLog.Add(evt);
            }

            return changeLog;
        }

        private async Task<List<IDomainEvent>> LoadAllChangeLogs()
        {
            var changeLog = new List<IDomainEvent>();
            var page = await _streamStore.ReadAllForwards(0, int.MaxValue);
            foreach (var message in page.Messages)
            {
                var json = await message.GetJsonData();
                var evt = (IDomainEvent) JsonConvert.DeserializeObject(json, Type.GetType(message.Type));
                changeLog.Add(evt);
            }

            return changeLog;
        }

        public class Db : EventSourcedDbContext
        {
            public Db(string connectionString, IStreamStore streamStore) : base(connectionString, streamStore)
            {
            }

            public DbSet<UserAggregate> Users { get; set; }
        }
    }
}
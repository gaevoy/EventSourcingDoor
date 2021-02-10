using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Utils;
using FluentAssertions;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.PollingClient;
using NEventStore.Serialization.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace EventSourcingDoor.Tests.NEventStoreOutbox.EntityFramework
{
    [Parallelizable(ParallelScope.None)]
    public class OutboxTests
    {
        private static Randomizer Random => TestContext.CurrentContext.Random;
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IStoreEvents _eventStore;

        [SetUp]
        public void EnsureSchemaInitialized()
        {
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(null, "System.Data.SqlClient", ConnectionString)
                .WithDialect(new MsSqlDialect())
                .InitializeStorageEngine()
                .UsingJsonSerialization()
                .Build();
            eventStore.Advanced.Purge();
            _eventStore = eventStore;
            var db = new TestDbContextWithOutbox(ConnectionString, _eventStore);
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
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Then
            db.DetachAll();
            var savedUser = await db.Users.FindAsync(user.Id);
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
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // When
            user.Rename("James Bond");
            var changeLog = user.GetUncommittedChanges().ToList();
            await db.SaveChangesAsync();

            // Then
            db.DetachAll();
            var savedUser = await db.Users.FindAsync(user.Id);
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
        public async Task It_should_throw_concurrency_error_via_EntityFramework()
        {
            // Given
            using var db = NewDbContext();
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            using var db1 = new TestDbContextWithOutbox(ConnectionString, _eventStore);
            using var db2 = new TestDbContextWithOutbox(ConnectionString, _eventStore);

            // When
            var user1 = await db1.Users.FindAsync(user.Id);
            user1.Rename("James Bond #1");
            var user2 = await db2.Users.FindAsync(user.Id);
            user2.Rename("James Bond #2");
            var changeLog = user2.Changes.GetUncommittedChanges().ToList();
            await db2.SaveChangesAsync();
            Func<Task> act = ()=> db1.SaveChangesAsync();

            // Then
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            db1.DetachAll();
            var savedUser = await db1.Users.FindAsync(user.Id);
            var savedChangeLog = (await LoadChangeLog(user.StreamId)).AsEnumerable()
                .Reverse()
                .Take(changeLog.Count)
                .Reverse()
                .ToList();
            savedUser.Should().BeEquivalentTo(user2);
            for (var i = 0; i < savedChangeLog.Count; i++)
            {
                savedChangeLog[i].Should().BeEquivalentTo((object) changeLog[i]);
                savedChangeLog[i].Should().BeOfType(changeLog[i].GetType());
            }
        }

        [Test]
        public async Task It_should_not_fail_if_there_is_no_changes()
        {
            // Given
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // When
            db.DetachAll();
            db.Entry(user).State = EntityState.Modified;
            Func<Task> act = () => db.SaveChangesAsync();

            // Then
            act.Should().NotThrow();
        }

        [Test]
        public async Task It_should_rollback_both_entity_and_change_log()
        {
            // Given
            var id = Guid.NewGuid();
            var user = new UserAggregate(id, "Bond");
            var changeLog = user.GetUncommittedChanges().ToList();
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // When
            using (var tran = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted))
            {
                user.Rename("James Bond");
                await db.SaveChangesAsync();
            }

            // Then
            db.DetachAll();
            var savedUser = await db.Users.FindAsync(id);
            var savedChangeLog = await LoadChangeLog(user.StreamId);
            savedUser.Should().BeEquivalentTo(new UserAggregate(id, "Bond"));
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
            var pollingClient = new PollingClient2(_eventStore.Advanced, commit =>
            {
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

        private TestDbContextWithOutbox NewDbContext()
        {
            return new TestDbContextWithOutbox(ConnectionString, _eventStore);
        }

        private async Task InsertUserInTransaction(UserAggregate user, Task beforeTransactionDone)
        {
            await Task.Yield();
            using var transaction = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted);
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await beforeTransactionDone;
            transaction.Complete();
        }

        private async Task<List<IDomainEvent>> LoadChangeLog(string streamId)
        {
            return _eventStore.Advanced.GetFrom(Bucket.Default, streamId, 0, int.MaxValue)
                .SelectMany(e => e.Events)
                .Select(e => e.Body)
                .OfType<IDomainEvent>()
                .ToList();
        }

        private async Task<List<IDomainEvent>> LoadAllChangeLogs()
        {
            return _eventStore.Advanced.GetFrom(0)
                .SelectMany(e => e.Events)
                .Select(e => e.Body)
                .OfType<IDomainEvent>()
                .ToList();
        }
    }
}
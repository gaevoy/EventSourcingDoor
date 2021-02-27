using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Utils;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;

#pragma warning disable 1998

namespace EventSourcingDoor.Tests.EF6_NEventStore_Sqlite
{
    public abstract class OutboxTestsBase
    {
        private static Randomizer Random => TestContext.CurrentContext.Random;
        protected IOutbox Outbox;

        [Test]
        public async Task It_should_insert_entity_and_record_change_log()
        {
            // Given
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var changeLog = user.Changes.GetUncommittedChanges().ToList();

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
            var changeLog = user.Changes.GetUncommittedChanges().ToList();
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
        public async Task It_should_delete_entity_and_record_change_log()
        {
            // Given
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // When
            user.Delete();
            var changeLog = user.Changes.GetUncommittedChanges().ToList();
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            // Then
            db.DetachAll();
            var savedUser = await db.Users.FindAsync(user.Id);
            var savedChangeLog = (await LoadChangeLog(user.StreamId)).AsEnumerable()
                .Reverse()
                .Take(changeLog.Count)
                .Reverse()
                .ToList();
            savedUser.Should().BeNull();
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
            using var db1 = NewDbContext();
            using var db2 = NewDbContext();

            // When
            var user1 = await db1.Users.FindAsync(user.Id);
            user1.Rename("James Bond #1");
            var user2 = await db2.Users.FindAsync(user.Id);
            user2.Rename("James Bond #2");
            var changeLog = user2.Changes.GetUncommittedChanges().ToList();
            await db2.SaveChangesAsync();
            Func<Task> act = () => db1.SaveChangesAsync();

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
        public async Task It_should_rollback_both_entity_and_change_log_within_async_flow()
        {
            // Given
            var id = Guid.NewGuid();
            var user = new UserAggregate(id, "Bond");
            var changeLog = user.Changes.GetUncommittedChanges().ToList();
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
            savedChangeLog.Should().HaveSameCount(changeLog);
            for (var i = 0; i < savedChangeLog.Count; i++)
            {
                savedChangeLog[i].Should().BeEquivalentTo((object) changeLog[i]);
                savedChangeLog[i].Should().BeOfType(changeLog[i].GetType());
            }
        }

        [Test]
        public async Task It_should_rollback_both_entity_and_change_log_within_sync_flow()
        {
            // Given
            var id = Guid.NewGuid();
            var user = new UserAggregate(id, "Bond");
            var changeLog = user.Changes.GetUncommittedChanges().ToList();
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // When
            using (var tran = TransactionExt.Begin(IsolationLevel.ReadCommitted))
            {
                user.Rename("James Bond");
                db.SaveChanges();
            }

            // Then
            db.DetachAll();
            var savedUser = await db.Users.FindAsync(id);
            var savedChangeLog = await LoadChangeLog(user.StreamId);
            savedUser.Should().BeEquivalentTo(new UserAggregate(id, "Bond"));
            savedChangeLog.Should().HaveSameCount(changeLog);
            for (var i = 0; i < savedChangeLog.Count; i++)
            {
                savedChangeLog[i].Should().BeEquivalentTo((object) changeLog[i]);
                savedChangeLog[i].Should().BeOfType(changeLog[i].GetType());
            }
        }

        [Test]
        [Ignore("To figure out why it fails on Sqlite")]
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

        protected async Task InsertUserInTransaction(UserAggregate user, Task beforeTransactionDone)
        {
            await Task.Yield();
            using var transaction = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted);
            using var db = NewDbContext();
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await beforeTransactionDone;
            transaction.Complete();
        }

        [Test]
        [Ignore("To figure out why it fails on Sqlite")]
        public async Task It_should_not_drop_events_during_reception()
        {
            // Given
            var events = new List<IDomainEvent>();
            var timeout = new CancellationTokenSource(20_000);
            var _ = Outbox.Receive((evt, _) =>
            {
                lock (events)
                    events.Add((IDomainEvent) evt);
            }, timeout.Token);
            await Task.Delay(1000);

            var user1 = new UserAggregate(Random.NextGuid(), "User#1");
            var user2 = new UserAggregate(Random.NextGuid(), "User#2");
            var longTransaction = new TaskCompletionSource<object>();
            timeout.Token.Register(() => longTransaction.TrySetResult(null));
            // Warm-up
            await InsertUserInTransaction(new UserAggregate(Random.NextGuid(), Random.GetString()), Task.CompletedTask);

            // When
            var longTask = InsertUserInTransaction(user1, longTransaction.Task);
            await Task.Delay(1000);
            var shortTask = InsertUserInTransaction(user2, Task.CompletedTask);
            await Task.Delay(1000);
            await shortTask;
            longTransaction.TrySetResult(null);
            await longTask;

            // Then
            await Task.Delay(5000);
            lock (events)
            {
                events.OfType<UserRegistered>().Should().Contain(e => e.Id == user1.Id);
                events.OfType<UserRegistered>().Should().Contain(e => e.Id == user2.Id);
            }
        }

        protected abstract TestDbContextWithOutbox NewDbContext();

        protected abstract Task<List<IDomainEvent>> LoadChangeLog(string streamId);

        protected abstract Task<List<IDomainEvent>> LoadAllChangeLogs();
    }
}
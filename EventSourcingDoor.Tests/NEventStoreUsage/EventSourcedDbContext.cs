using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EventSourcingDoor.Tests.Utils;
using NEventStore;

namespace EventSourcingDoor.Tests.NEventStoreUsage
{
    public abstract class EventSourcedDbContext : DbContext
    {
        private readonly IStoreEvents _eventStore;

        protected EventSourcedDbContext(string connectionString, IStoreEvents eventStore)
            : base(connectionString)
        {
            _eventStore = eventStore;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellation)
        {
            using var transaction = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted);
            var changeLogs = GetChangeLogs();
            var result = await base.SaveChangesAsync(cancellation);
            foreach (var changeLog in changeLogs)
            {
                using (var stream = _eventStore.OpenStream(changeLog.StreamId))
                {
                    foreach (var change in changeLog.GetUncommittedChanges())
                        stream.Add(new EventMessage {Body = change});
                    stream.CommitChanges(Guid.NewGuid());
                }

                changeLog.MarkChangesAsCommitted();
            }

            transaction.Complete();
            return result;
        }

        public override int SaveChanges()
        {
            using var transaction = TransactionExt.Begin(IsolationLevel.ReadCommitted);
            var changeLogs = GetChangeLogs();
            var result = base.SaveChanges();
            foreach (var changeLog in changeLogs)
            {
                using (var stream = _eventStore.OpenStream(changeLog.StreamId))
                {
                    foreach (var change in changeLog.GetUncommittedChanges())
                        stream.Add(new EventMessage {Body = change});
                    stream.CommitChanges(Guid.NewGuid());
                }

                changeLog.MarkChangesAsCommitted();
            }

            transaction.Complete();
            return result;
        }

        private IEnumerable<IChangeLog> GetChangeLogs()
        {
            return ChangeTracker
                .Entries()
                .Select(e => e.Entity)
                .Where(entity => entity is IChangeLog || entity is IHaveChangeLog)
                .Select(entity => entity is IHaveChangeLog container
                    ? container.Changes
                    : (IChangeLog) entity);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EventSourcingDoor.Tests.Utils;
using Newtonsoft.Json;
using SqlStreamStore;
using SqlStreamStore.Streams;

namespace EventSourcingDoor.Tests.SqlStreamStoreUsage
{
    public abstract class EventSourcedDbContext : DbContext
    {
        private readonly IStreamStore _streamStore;

        protected EventSourcedDbContext(string connectionString, IStreamStore streamStore)
            : base(connectionString)
        {
            _streamStore = streamStore;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellation)
        {
            using var transaction = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted);
            var changeLogs = GetChangeLogs();
            var result = await base.SaveChangesAsync(cancellation);
            foreach (var changeLog in changeLogs)
            {
                var streamMessages = changeLog
                    .GetUncommittedChanges()
                    .Select(e => new NewStreamMessage(
                        Guid.NewGuid(),
                        e.GetType().FullName,
                        JsonConvert.SerializeObject(e)))
                    .ToArray();
                await _streamStore.AppendToStream(
                    changeLog.StreamId,
                    ExpectedVersion.Any,
                    streamMessages, cancellation);
                changeLog.MarkChangesAsCommitted();
            }

            transaction.Complete();
            return result;
        }

        public override int SaveChanges()
        {
            return AsyncPump.Run(SaveChangesAsync);
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
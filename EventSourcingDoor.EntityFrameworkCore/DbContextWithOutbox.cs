using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.EntityFrameworkCore
{
    public abstract class DbContextWithOutbox : DbContext
    {
        private readonly IOutbox _outbox;

        protected DbContextWithOutbox(DbContextOptions options, IOutbox outbox) : base(options)
        {
            _outbox = outbox;
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChanges,
            CancellationToken cancellation = default)
        {
            using var transaction = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted);
            var changeLogs = GetChangeLogs();
            var result = await base.SaveChangesAsync(acceptAllChanges, cancellation);
            await _outbox.SendAsync(changeLogs, cancellation);
            transaction.Complete();
            return result;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            using var transaction = TransactionExt.Begin(IsolationLevel.ReadCommitted);
            var changeLogs = GetChangeLogs();
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            _outbox.Send(changeLogs);
            transaction.Complete();
            return result;
        }

        private List<IChangeLog> GetChangeLogs()
        {
            return ChangeTracker
                .Entries()
                .Select(e => e.Entity)
                .Where(entity => entity is IChangeLog || entity is IHaveChangeLog)
                .Select(entity => entity is IHaveChangeLog container
                    ? container.Changes
                    : (IChangeLog) entity)
                .ToList();
        }
    }
}
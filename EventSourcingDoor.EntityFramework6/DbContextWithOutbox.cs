using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace EventSourcingDoor.EntityFramework6
{
    public abstract class DbContextWithOutbox : DbContext
    {
        private readonly IOutbox _outbox;

        protected DbContextWithOutbox(string connectionString, IOutbox outbox)
            : base(connectionString)
        {
            _outbox = outbox;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellation)
        {
            using var transaction = TransactionExt.BeginAsync(IsolationLevel.ReadCommitted);
            var changeLogs = GetChangeLogs();
            var result = await base.SaveChangesAsync(cancellation);
            await _outbox.SendAsync(changeLogs, cancellation);
            transaction.Complete();
            return result;
        }

        public override int SaveChanges()
        {
            using var transaction = TransactionExt.Begin(IsolationLevel.ReadCommitted);
            var changeLogs = GetChangeLogs();
            var result = base.SaveChanges();
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
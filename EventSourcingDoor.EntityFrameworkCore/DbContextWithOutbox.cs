using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

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
            IDbContextTransaction transaction = null;
            if (Database.CurrentTransaction == null)
                transaction = await Database
                    .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellation);
            await using (transaction)
            {
                var changeLogs = GetChangeLogs();
                var result = await base.SaveChangesAsync(acceptAllChanges, cancellation);
                // AmbientContext.CurrentTransaction = Database.CurrentTransaction?.GetDbTransaction();
                AmbientContext.CurrentConnection = Database.GetDbConnection();
                // AmbientContext.CurrentTransaction = Database.CurrentTransaction.GetDbTransaction();
                _outbox.Send(changeLogs);
                AmbientContext.CurrentTransaction = null;
                AmbientContext.CurrentConnection = null;
                if (transaction != null)
                    await transaction.CommitAsync(cancellation);
                return result;
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            IDbContextTransaction transaction = null;
            if (Database.CurrentTransaction == null)
                transaction = Database.BeginTransaction(IsolationLevel.ReadCommitted);
            using (transaction)
            {
                var changeLogs = GetChangeLogs();
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                // AmbientContext.CurrentTransaction = Database.CurrentTransaction?.GetDbTransaction();
                AmbientContext.CurrentConnection = Database.GetDbConnection();
                // AmbientContext.CurrentTransaction = Database.CurrentTransaction.GetDbTransaction();
                _outbox.Send(changeLogs);
                AmbientContext.CurrentTransaction = null;
                AmbientContext.CurrentConnection = null;
                transaction?.Commit();
                return result;
            }
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
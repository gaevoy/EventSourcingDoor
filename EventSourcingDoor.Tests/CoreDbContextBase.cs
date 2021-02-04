using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcingDoor.Tests
{
    public abstract class CoreDbContextBase : DbContext
    {
        private readonly ICapPublisher _capPublisher;

        protected CoreDbContextBase(DbContextOptions options, ICapPublisher capPublisher)
            : base(options)
        {
            _capPublisher = capPublisher;
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChanges,
            CancellationToken cancellation = default)
        {
            var transaction = Database.CurrentTransaction == null
                ? await Database.BeginTransactionAsync(cancellation)
                : null;
            transaction = WrapInCapTransaction(transaction);
            using (transaction)
            {
                var changeLogs = GetChangeLogs();
                var result = await base.SaveChangesAsync(acceptAllChanges, cancellation);
                foreach (var changeLog in changeLogs)
                {
                    foreach (var evt in changeLog.GetUncommittedChanges())
                        await _capPublisher.PublishAsync("", evt, cancellationToken: cancellation);
                    changeLog.MarkChangesAsCommitted();
                }

                if (transaction != null) await transaction.CommitAsync(cancellation);
                return result;
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var transaction = Database.CurrentTransaction == null
                ? Database.BeginTransaction()
                : null;
            transaction = WrapInCapTransaction(transaction);
            using (transaction)
            {
                var changeLogs = GetChangeLogs();
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                foreach (var changeLog in changeLogs)
                {
                    foreach (var evt in changeLog.GetUncommittedChanges())
                        _capPublisher.Publish("", evt);
                    changeLog.MarkChangesAsCommitted();
                }

                transaction?.Commit();
                return result;
            }
        }

        private IDbContextTransaction WrapInCapTransaction(IDbContextTransaction transaction)
        {
            var capTransaction = _capPublisher.ServiceProvider.GetService<ICapTransaction>();
            capTransaction.Begin(Database.CurrentTransaction ?? transaction);
            _capPublisher.Transaction.Value = capTransaction;
            if (transaction != null)
                transaction = new CapEFDbTransaction(capTransaction);
            return transaction;
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
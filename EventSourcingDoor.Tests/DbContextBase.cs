using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcingDoor.Tests
{
    public abstract class DbContextBase : DbContext
    {
        private readonly ICapPublisher _capPublisher;

        protected DbContextBase(string connectionString, ICapPublisher capPublisher)
            : base(connectionString)
        {
            _capPublisher = capPublisher;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellation)
        {
            var transaction = Database.CurrentTransaction == null
                ? Database.BeginTransaction()
                : null;
            // https://docs.microsoft.com/en-au/ef/ef6/saving/transactions?redirectedfrom=MSDN
            throw new NotImplementedException("check async tran");
            AttachCapToTransaction(Database.CurrentTransaction ?? transaction);
            using (transaction)
            {
                var changeLogs = GetChangeLogs();
                var result = await base.SaveChangesAsync(cancellation);
                foreach (var changeLog in changeLogs)
                {
                    foreach (var evt in changeLog.GetUncommittedChanges())
                        await _capPublisher.PublishAsync("", evt, cancellationToken: cancellation);
                    changeLog.MarkChangesAsCommitted();
                }

                transaction?.Commit();
                return result;
            }
        }

        public override int SaveChanges()
        {
            var transaction = Database.CurrentTransaction == null
                ? Database.BeginTransaction()
                : null;
            AttachCapToTransaction(Database.CurrentTransaction ?? transaction);
            using (transaction)
            {
                var changeLogs = GetChangeLogs();
                var result = base.SaveChanges();
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

        private void AttachCapToTransaction(DbContextTransaction transaction)
        {
            var capTransaction = _capPublisher.ServiceProvider.GetService<ICapTransaction>();
            capTransaction.Begin(transaction.UnderlyingTransaction);
            _capPublisher.Transaction.Value = capTransaction;
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
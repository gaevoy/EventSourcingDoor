using System;
using System.Transactions;
using static System.Transactions.IsolationLevel;
using static System.Transactions.TransactionScopeOption;

namespace EventSourcingDoor.EntityFrameworkCore
{
    public static class TransactionExt
    {
        public static TransactionScope BeginAsync(
            IsolationLevel isolationLevel = Serializable,
            TimeSpan timeout = default
        )
        {
            var options = new TransactionOptions {IsolationLevel = isolationLevel, Timeout = timeout};
            return new TransactionScope(Required, options, TransactionScopeAsyncFlowOption.Enabled);
        }

        public static TransactionScope Begin(
            IsolationLevel isolationLevel = Serializable,
            TimeSpan timeout = default
        )
        {
            var options = new TransactionOptions {IsolationLevel = isolationLevel, Timeout = timeout};
            return new TransactionScope(Required, options);
        }
    }
}
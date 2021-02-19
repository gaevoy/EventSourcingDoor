using System;
using System.Transactions;

namespace EventSourcingDoor.EntityFramework6
{
    public static class TransactionExt
    {
        public static TransactionScope BeginAsync(
            IsolationLevel isolationLevel = IsolationLevel.Serializable,
            TimeSpan timeout = default
        )
        {
            var options = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = timeout
            };
            return new TransactionScope(
                TransactionScopeOption.Required,
                options,
                TransactionScopeAsyncFlowOption.Enabled
            );
        }

        public static TransactionScope Begin(
            IsolationLevel isolationLevel = IsolationLevel.Serializable,
            TimeSpan timeout = default
        )
        {
            var options = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
                Timeout = timeout
            };
            return new TransactionScope(
                TransactionScopeOption.Required,
                options
            );
        }
    }
}
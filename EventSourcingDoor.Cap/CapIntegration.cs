using System;
using System.Data.Common;
using DotNetCore.CAP;

namespace EventSourcingDoor.Cap
{
    public class CapIntegration<TEventBase>
    {
        private readonly ICapPublisher _publisher;

        public CapIntegration(ICapPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Flush(IHaveChangeLog<TEventBase> changeLog)
        {
            foreach (var evt in changeLog.Changes.GetUncommittedChanges())
                _publisher.Publish("", evt);
            changeLog.Changes.MarkChangesAsCommitted();
        }

        public void Flush(IHaveChangeLog<TEventBase> changeLog, DbConnection connection)
        {
            using (var transaction = connection.BeginTransaction(_publisher))
            {
                foreach (var evt in changeLog.Changes.GetUncommittedChanges())
                    _publisher.Publish("", evt);
                transaction.Commit();
                changeLog.Changes.MarkChangesAsCommitted();
            }
        }
    }
}
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

        public void Flush(IHasEventStream<TEventBase> eventStream)
        {
            foreach (var evt in eventStream.EventStream.GetUncommittedChanges())
                _publisher.Publish("", evt);
            eventStream.EventStream.MarkChangesAsCommitted();
        }

        public void Flush(IHasEventStream<TEventBase> eventStream, DbConnection connection)
        {
            using (var transaction = connection.BeginTransaction(_publisher))
            {
                foreach (var evt in eventStream.EventStream.GetUncommittedChanges())
                    _publisher.Publish("", evt);
                transaction.Commit();
                eventStream.EventStream.MarkChangesAsCommitted();
            }
        }
    }
}
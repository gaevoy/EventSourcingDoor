using System.Collections.Generic;

namespace EventSourcingDoor
{
    public abstract class AggregateBase<TAggregate, TEventBase> :
        IHasEventStream<TEventBase>,
        IAmEventStream<TEventBase>
        where TAggregate : AggregateBase<TAggregate, TEventBase>
    {
        public IAmEventStream<TEventBase> EventStream => _eventStream;
        protected abstract StreamDefinition<TAggregate, TEventBase> Definition { get; }
        private readonly EventStream<TAggregate, TEventBase> _eventStream;

        protected AggregateBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            _eventStream = Definition.New((TAggregate) this);
        }

        protected void ApplyChange(TEventBase evt) => _eventStream.ApplyChange(evt);

        public IEnumerable<TEventBase> GetUncommittedChanges() => _eventStream.GetUncommittedChanges();

        public void MarkChangesAsCommitted() => _eventStream.MarkChangesAsCommitted();

        public void LoadFromHistory(IEnumerable<TEventBase> history) => _eventStream.LoadFromHistory(history);
    }
}
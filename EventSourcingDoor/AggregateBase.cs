using System.Collections.Generic;

namespace EventSourcingDoor
{
    public abstract class AggregateBase<TAggregate, TEventBase> :
        IHaveEventStream<TEventBase>,
        IAmEventStream<TEventBase>
        where TAggregate : AggregateBase<TAggregate, TEventBase>
    {
        protected abstract StreamDefinition<TAggregate, TEventBase> Definition { get; }
        private readonly EventStream<TAggregate, TEventBase> _eventStream;
        public IAmEventStream<TEventBase> EventStream => _eventStream;

        protected AggregateBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            _eventStream = Definition.New((TAggregate) this);
        }

        public IEnumerable<TEventBase> GetUncommittedChanges() => _eventStream.GetUncommittedChanges();

        public void MarkChangesAsCommitted() => _eventStream.MarkChangesAsCommitted();

        public void LoadFromHistory(IEnumerable<TEventBase> history) => _eventStream.LoadFromHistory(history);

        protected void ApplyChange(TEventBase evt) => _eventStream.ApplyChange(evt);
    }
}
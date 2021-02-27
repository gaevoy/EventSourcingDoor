using IEvent = System.Object;

namespace EventSourcingDoor
{
    public abstract class AggregateBase<TAggregate, TEventBase> :
        IHaveChangeLog
        where TAggregate : AggregateBase<TAggregate, TEventBase>
    {
        protected abstract ChangeLogDefinition<TAggregate> Definition { get; }
        private readonly ChangeLog<TAggregate, TEventBase> _changes;
        public IChangeLog Changes => _changes;
        public abstract string StreamId { get; }

        protected AggregateBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            _changes = Definition.New<TEventBase>((TAggregate) this);
        }

        protected void ApplyChange(TEventBase evt)
            => _changes.ApplyChange(evt);
    }
}
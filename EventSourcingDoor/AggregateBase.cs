using System.Collections.Generic;

namespace EventSourcingDoor
{
    public abstract class AggregateBase<TAggregate, TEventBase> :
        IHaveChangeLog<TEventBase>,
        IChangeLog<TEventBase>
        where TAggregate : AggregateBase<TAggregate, TEventBase>
    {
        protected abstract ChangeLogDefinition<TAggregate, TEventBase> Definition { get; }
        private readonly ChangeLog<TAggregate, TEventBase> _changes;
        public IChangeLog<TEventBase> Changes => _changes;

        protected AggregateBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            _changes = Definition.New((TAggregate) this);
        }

        public IEnumerable<TEventBase> GetUncommittedChanges() => _changes.GetUncommittedChanges();

        public void MarkChangesAsCommitted() => _changes.MarkChangesAsCommitted();

        public void LoadFromHistory(IEnumerable<TEventBase> history) => _changes.LoadFromHistory(history);

        protected void ApplyChange(TEventBase evt) => _changes.ApplyChange(evt);
    }
}
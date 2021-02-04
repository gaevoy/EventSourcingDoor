using System.Collections.Generic;
using System.Linq;
using IEvent = System.Object;

namespace EventSourcingDoor
{
    public abstract class AggregateBase<TAggregate, TEventBase> :
        IHaveChangeLog,
        IChangeLog
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

        public IEnumerable<TEventBase> GetUncommittedChanges()
            => _changes.GetUncommittedChanges();


        public void MarkChangesAsCommitted()
            => _changes.MarkChangesAsCommitted();

        public void LoadFromHistory(IEnumerable<TEventBase> history)
            => _changes.LoadFromHistory(history);

        protected void ApplyChange(TEventBase evt)
            => _changes.ApplyChange(evt);

        void IChangeLog.LoadFromHistory(IEnumerable<IEvent> history)
            => _changes.LoadFromHistory(history);

        IEnumerable<IEvent> IChangeLog.GetUncommittedChanges()
            => _changes.GetUncommittedChanges().Cast<IEvent>();
    }
}
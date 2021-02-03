using System.Collections.Generic;

namespace EventSourcingDoor
{
    public interface IAmEventStream<TEventBase>
    {
        IEnumerable<TEventBase> GetUncommittedChanges();
        void MarkChangesAsCommitted();
        void LoadFromHistory(IEnumerable<TEventBase> history);
    }
}
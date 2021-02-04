using System.Collections.Generic;

namespace EventSourcingDoor
{
    public interface IChangeLog<TEventBase>
    {
        IEnumerable<TEventBase> GetUncommittedChanges();
        void MarkChangesAsCommitted();
        void LoadFromHistory(IEnumerable<TEventBase> history);
    }
}
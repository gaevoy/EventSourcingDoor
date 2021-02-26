using System.Collections.Generic;
using IEvent = System.Object;

namespace EventSourcingDoor
{
    public interface IChangeLog
    {
        string StreamId { get; }
        IEnumerable<IEvent> GetUncommittedChanges();
        void MarkChangesAsCommitted();
        void LoadFromHistory(IEnumerable<IEvent> history);
    }
}
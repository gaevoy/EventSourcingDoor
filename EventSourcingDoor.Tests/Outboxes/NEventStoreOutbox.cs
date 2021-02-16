using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NEventStore;

namespace EventSourcingDoor.Tests.Outboxes
{
    public class NEventStoreOutbox : IOutbox
    {
        private readonly IStoreEvents _eventStore;

        public NEventStoreOutbox(IStoreEvents eventStore)
        {
            _eventStore = eventStore;
        }

        public void SaveChanges(IEnumerable<IChangeLog> changes)
        {
            foreach (var changeLog in changes)
            {
                using (var stream = _eventStore.OpenStream(changeLog.StreamId))
                {
                    foreach (var change in changeLog.GetUncommittedChanges())
                        stream.Add(new EventMessage {Body = change});
                    stream.CommitChanges(Guid.NewGuid());
                }

                changeLog.MarkChangesAsCommitted();
            }
        }

        public Task SaveChangesAsync(IEnumerable<IChangeLog> changes, CancellationToken cancellation)
        {
            SaveChanges(changes);
            return Task.CompletedTask;
        }
    }
}
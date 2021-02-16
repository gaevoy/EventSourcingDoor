using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcingDoor.Tests.Utils;
using Newtonsoft.Json;
using SqlStreamStore;
using SqlStreamStore.Streams;

namespace EventSourcingDoor.Tests.Outboxes
{
    public class SqlStreamStoreOutbox : IOutbox
    {
        private readonly IStreamStore _eventStore;

        public SqlStreamStoreOutbox(IStreamStore eventStore)
        {
            _eventStore = eventStore;
        }

        public void SaveChanges(IEnumerable<IChangeLog> changes)
        {
            AsyncPump.Run(() => SaveChangesAsync(changes, CancellationToken.None));
        }

        public async Task SaveChangesAsync(IEnumerable<IChangeLog> changes, CancellationToken cancellation)
        {
            foreach (var changeLog in changes)
            {
                var streamMessages = changeLog
                    .GetUncommittedChanges()
                    .Select(e => new NewStreamMessage(
                        Guid.NewGuid(),
                        e.GetType().FullName,
                        JsonConvert.SerializeObject(e)))
                    .ToArray();
                await _eventStore.AppendToStream(
                    changeLog.StreamId,
                    ExpectedVersion.Any,
                    streamMessages, cancellation);
                changeLog.MarkChangesAsCommitted();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SqlStreamStore;
using SqlStreamStore.Streams;

namespace EventSourcingDoor.SqlStreamStore
{
    public class SqlStreamStoreOutbox : IOutbox
    {
        private readonly IStreamStore _eventStore;
        private readonly TimeSpan _receptionDelay;

        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };

        public SqlStreamStoreOutbox(IStreamStore eventStore, TimeSpan receptionDelay)
        {
            _eventStore = eventStore;
            _receptionDelay = receptionDelay;
        }

        public void Send(IEnumerable<IChangeLog> changes)
        {
            AsyncPump.Run(() => SendAsync(changes, CancellationToken.None));
        }

        public async Task SendAsync(IEnumerable<IChangeLog> changes, CancellationToken cancellation)
        {
            foreach (var changeLog in changes)
            {
                var streamMessages = changeLog
                    .GetUncommittedChanges()
                    .Select(e => new NewStreamMessage(
                        Guid.NewGuid(),
                        e.GetType().FullName,
                        JsonConvert.SerializeObject(e, SerializerSettings)))
                    .ToArray();
                await _eventStore.AppendToStream(
                    changeLog.StreamId ?? Guid.NewGuid().ToString(),
                    ExpectedVersion.Any,
                    streamMessages, cancellation);
                changeLog.MarkChangesAsCommitted();
            }
        }

        public async Task Receive(Action<object> onReceived, CancellationToken cancellation)
        {
            // TODO: Make use of `message.Position`
            var cancelling = new TaskCompletionSource<object>();
            cancellation.Register(() => cancelling.SetResult(null));
            var streamStore = new OutboxAwareStreamStore(_eventStore, _receptionDelay);
            using (streamStore.SubscribeToAll(null, ReceiveEvent))
                await cancelling.Task;

            async Task ReceiveEvent(IAllStreamSubscription _, StreamMessage message, CancellationToken __)
            {
                var json = await message.GetJsonData();
                var evt = JsonConvert.DeserializeObject(json, SerializerSettings);
                onReceived(evt);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;

namespace EventSourcingDoor.Tests.Cap
{
    public class CapOutbox : IOutbox
    {
        private readonly ICapPublisher _capPublisher;
        private readonly CapInMemoryTransport _transport;

        public CapOutbox(ICapPublisher capPublisher, CapInMemoryTransport transport)
        {
            _capPublisher = capPublisher;
            _transport = transport;
        }

        public void Send(IEnumerable<IChangeLog> changes)
        {
            foreach (var changeLog in changes)
            {
                var streamId = changeLog.StreamId ?? Guid.NewGuid().ToString();
                foreach (var change in changeLog.GetUncommittedChanges())
                {
                    var headers = new Dictionary<string, string>()
                    {
                        {"DotNetType", change.GetType().FullName},
                        {"StreamId", streamId}
                    };
                    _capPublisher.Publish("outbox", change, headers);
                }

                changeLog.MarkChangesAsCommitted();
            }
        }

        public async Task SendAsync(IEnumerable<IChangeLog> changes, CancellationToken cancellation)
        {
            foreach (var changeLog in changes)
            {
                var streamId = changeLog.StreamId ?? Guid.NewGuid().ToString();
                foreach (var change in changeLog.GetUncommittedChanges())
                {
                    var headers = new Dictionary<string, string>()
                    {
                        {"DotNetType", change.GetType().FullName},
                        {"StreamId", streamId}
                    };
                    await _capPublisher.PublishAsync("outbox", change, headers, cancellation);
                }

                changeLog.MarkChangesAsCommitted();
            }
        }

        public Task Receive(Action<object> onReceived, CancellationToken cancellation)
        {
            return _transport.Subscribe(e => { onReceived(e.Event); }, cancellation);
        }
    }
}
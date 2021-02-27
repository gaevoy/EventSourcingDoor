using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NEventStore;
using NEventStore.PollingClient;
using IEvent = System.Object;
using IReceptionContext = System.Object;
using ICheckpoint = System.Object;

namespace EventSourcingDoor.NEventStore
{
    public class NEventStoreOutbox : IOutbox
    {
        private readonly IStoreEvents _eventStore;
        private readonly TimeSpan _receptionDelay;

        /// <summary>
        /// Create `NEventStore` implementation of `IOutbox`.
        /// </summary>
        /// <param name="eventStore">NEventStore instance</param>
        /// <param name="receptionDelay">
        /// It should include transaction timeout + clock drift.
        /// Otherwise, it may skip events during reception.
        /// </param>
        public NEventStoreOutbox(IStoreEvents eventStore, TimeSpan receptionDelay)
        {
            _eventStore = eventStore;
            _receptionDelay = receptionDelay;
        }

        public void Send(IEnumerable<IChangeLog> changes)
        {
            foreach (var changeLog in changes)
            {
                var streamId = changeLog.StreamId ?? Guid.NewGuid().ToString();
                using (var stream = _eventStore.OpenStream(streamId))
                {
                    foreach (var change in changeLog.GetUncommittedChanges())
                        stream.Add(new EventMessage {Body = change});
                    stream.CommitChanges(Guid.NewGuid());
                }

                changeLog.MarkChangesAsCommitted();
            }
        }

        public Task SendAsync(IEnumerable<IChangeLog> changes, CancellationToken cancellation)
        {
            Send(changes);
            return Task.CompletedTask;
        }

        public async Task Receive(
            Action<IEvent, IReceptionContext> onReceived,
            CancellationToken cancellation,
            ICheckpoint checkpoint)
        {
            var cancelling = new TaskCompletionSource<object>();
            cancellation.Register(() => cancelling.SetResult(null));
            using (var pollingClient = new PollingClient2(_eventStore.Advanced, OnCommitReceived))
            {
                pollingClient.StartFrom((long?) checkpoint ?? 0);
                await cancelling.Task;
            }

            PollingClient2.HandlingResult OnCommitReceived(ICommit commit)
            {
                var visibilityDate = DateTime.UtcNow - _receptionDelay;
                if (commit.CommitStamp > visibilityDate)
                    return PollingClient2.HandlingResult.Retry; // Wait more for the guaranteed reception delay
                foreach (var evt in commit.Events)
                    onReceived(evt.Body, commit);

                return PollingClient2.HandlingResult.MoveToNext;
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SqlStreamStore;
using SqlStreamStore.Infrastructure;
using SqlStreamStore.Streams;
using SqlStreamStore.Subscriptions;

namespace EventSourcingDoor.Tests.SqlStreamStoreOutbox.EntityFramework
{
    public class OutboxAwareStreamStore : IStreamStore
    {
        private readonly IStreamStore _store;
        private readonly TimeSpan _guaranteedDelay;

        public OutboxAwareStreamStore(IStreamStore store, TimeSpan guaranteedDelay)
        {
            _store = store;
            _guaranteedDelay = guaranteedDelay;
        }
        public void Dispose()
        {
            _store.Dispose();
        }

        public async Task<ReadAllPage> ReadAllForwards(
            long fromPositionInclusive, 
            int maxCount,
            bool prefetchJsonData = true,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var visibilityDate = DateTime.UtcNow - _guaranteedDelay;
            var page = await _store
                .ReadAllForwards(fromPositionInclusive, maxCount, prefetchJsonData, cancellationToken)
                .NotOnCapturedContext();
            if (page.Messages.Any())
            {
                var messageDate = page.Messages.Select(e => DateTime.SpecifyKind(e.CreatedUtc, DateTimeKind.Utc)).Max();
                if (messageDate > visibilityDate)
                {
                    await Task.Delay(messageDate - visibilityDate, cancellationToken).NotOnCapturedContext();
                    await _store
                        .ReadAllForwards(fromPositionInclusive, maxCount, prefetchJsonData, cancellationToken)
                        .NotOnCapturedContext();
                }
            }

            return page;
        }

        public Task<ReadAllPage> ReadAllBackwards(long fromPositionInclusive, int maxCount, bool prefetchJsonData = true,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ReadAllBackwards(fromPositionInclusive, maxCount, prefetchJsonData, cancellationToken);
        }

        public Task<ReadStreamPage> ReadStreamForwards(StreamId streamId, int fromVersionInclusive, int maxCount, bool prefetchJsonData = true,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ReadStreamForwards(streamId, fromVersionInclusive, maxCount, prefetchJsonData, cancellationToken);
        }

        public Task<ReadStreamPage> ReadStreamBackwards(StreamId streamId, int fromVersionInclusive, int maxCount, bool prefetchJsonData = true,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ReadStreamBackwards(streamId, fromVersionInclusive, maxCount, prefetchJsonData, cancellationToken);
        }

        public IStreamSubscription SubscribeToStream(StreamId streamId, int? continueAfterVersion,
            StreamMessageReceived streamMessageReceived, SubscriptionDropped subscriptionDropped = null,
            HasCaughtUp hasCaughtUp = null, bool prefetchJsonData = true, string name = null)
        {
            return _store.SubscribeToStream(streamId, continueAfterVersion, streamMessageReceived, subscriptionDropped, hasCaughtUp, prefetchJsonData, name);
        }

        public IAllStreamSubscription SubscribeToAll(long? continueAfterPosition, AllStreamMessageReceived streamMessageReceived,
            AllSubscriptionDropped subscriptionDropped = null, HasCaughtUp hasCaughtUp = null, bool prefetchJsonData = true,
            string name = null)
        {
            return new AllStreamSubscription(
                continueAfterPosition,
                this,
                new PollingStreamStoreNotifier(this),
                streamMessageReceived,
                subscriptionDropped,
                hasCaughtUp,
                prefetchJsonData,
                name);
        }

        public Task<long> ReadHeadPosition(CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ReadHeadPosition(cancellationToken);
        }

        public Task<long> ReadStreamHeadPosition(StreamId streamId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ReadStreamHeadPosition(streamId, cancellationToken);
        }

        public Task<int> ReadStreamHeadVersion(StreamId streamId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ReadStreamHeadVersion(streamId, cancellationToken);
        }

        public Task<StreamMetadataResult> GetStreamMetadata(string streamId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.GetStreamMetadata(streamId, cancellationToken);
        }

        public Task<ListStreamsPage> ListStreams(int maxCount = 100, string continuationToken = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ListStreams(maxCount, continuationToken, cancellationToken);
        }

        public Task<ListStreamsPage> ListStreams(Pattern pattern, int maxCount = 100, string continuationToken = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.ListStreams(pattern, maxCount, continuationToken, cancellationToken);
        }

        public event Action OnDispose
        {
            add => _store.OnDispose += value;
            remove => _store.OnDispose -= value;
        }

        public Task<AppendResult> AppendToStream(StreamId streamId, int expectedVersion, NewStreamMessage[] messages,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.AppendToStream(streamId, expectedVersion, messages, cancellationToken);
        }

        public Task DeleteStream(StreamId streamId, int expectedVersion = -2,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.DeleteStream(streamId, expectedVersion, cancellationToken);
        }

        public Task DeleteMessage(StreamId streamId, Guid messageId, CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.DeleteMessage(streamId, messageId, cancellationToken);
        }

        public Task SetStreamMetadata(StreamId streamId, int expectedStreamMetadataVersion = -2, int? maxAge = null,
            int? maxCount = null, string metadataJson = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return _store.SetStreamMetadata(streamId, expectedStreamMetadataVersion, maxAge, maxCount, metadataJson, cancellationToken);
        }
    }
}
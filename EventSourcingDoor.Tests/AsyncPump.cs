using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcingDoor.Tests
{
    public static class AsyncPump
    {
        public static void Run(Action asyncMethod)
        {
            if (asyncMethod == null) throw new ArgumentNullException(nameof(asyncMethod));

            SynchronizationContext prevCtx = SynchronizationContext.Current;
            try
            {
                // Establish the new context
                var syncCtx = new SingleThreadSynchronizationContext(true);
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                // Invoke the function
                syncCtx.OperationStarted();
                asyncMethod();
                syncCtx.OperationCompleted();

                // Pump continuations and propagate any exceptions
                syncCtx.RunOnCurrentThread();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }

        public static void Run(Func<Task> asyncMethod)
        {
            if (asyncMethod == null) throw new ArgumentNullException(nameof(asyncMethod));

            SynchronizationContext prevCtx = SynchronizationContext.Current;
            try
            {
                // Establish the new context
                var syncCtx = new SingleThreadSynchronizationContext(false);
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                // Invoke the function and alert the context to when it completes
                Task t = asyncMethod();
                if (t == null) throw new InvalidOperationException("No task provided.");
                t.ContinueWith(delegate { syncCtx.Complete(); }, TaskScheduler.Default);

                // Pump continuations and propagate any exceptions
                syncCtx.RunOnCurrentThread();
                t.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }

        public static T Run<T>(Func<Task<T>> asyncMethod)
        {
            if (asyncMethod == null) throw new ArgumentNullException(nameof(asyncMethod));

            SynchronizationContext prevCtx = SynchronizationContext.Current;
            try
            {
                // Establish the new context
                var syncCtx = new SingleThreadSynchronizationContext(false);
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                // Invoke the function and alert the context to when it completes
                Task<T> t = asyncMethod();
                if (t == null) throw new InvalidOperationException("No task provided.");
                t.ContinueWith(delegate { syncCtx.Complete(); }, TaskScheduler.Default);

                // Pump continuations and propagate any exceptions
                syncCtx.RunOnCurrentThread();
                return t.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }

        private sealed class SingleThreadSynchronizationContext : SynchronizationContext
        {
            private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> _queue =
                new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

            private readonly bool _trackOperations;

            private int _operationCount;

            internal SingleThreadSynchronizationContext(bool trackOperations)
            {
                _trackOperations = trackOperations;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                if (d == null) throw new ArgumentNullException(nameof(d));
                _queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("Synchronously sending is not supported.");
            }

            public override void OperationStarted()
            {
                if (_trackOperations)
                    Interlocked.Increment(ref _operationCount);
            }

            public override void OperationCompleted()
            {
                if (_trackOperations &&
                    Interlocked.Decrement(ref _operationCount) == 0)
                    Complete();
            }

            public void RunOnCurrentThread()
            {
                foreach (KeyValuePair<SendOrPostCallback, object> workItem in _queue.GetConsumingEnumerable())
                {
                    workItem.Key(workItem.Value);
                }
            }

            public void Complete()
            {
                _queue.CompleteAdding();
            }
        }
    }
}
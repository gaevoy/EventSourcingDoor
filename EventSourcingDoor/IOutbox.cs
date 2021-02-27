using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IEvent = System.Object;
using IReceptionContext = System.Object;
using ICheckpoint = System.Object;

namespace EventSourcingDoor
{
    public interface IOutbox
    {
        void Send(IEnumerable<IChangeLog> changes);
        Task SendAsync(IEnumerable<IChangeLog> changes, CancellationToken cancellation);

        Task Receive(
            Action<IEvent, IReceptionContext> onReceived,
            CancellationToken cancellation,
            ICheckpoint checkpoint = null);
    }
}
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;

namespace EventSourcingDoor.Tests.Cap
{
    public class CapNullDispatcher : IDispatcher
    {
        public void EnqueueToPublish(MediumMessage message)
        {
        }

        public void EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor)
        {
        }
    }
}
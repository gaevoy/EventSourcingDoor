using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Newtonsoft.Json;

namespace EventSourcingDoor.Tests.Outboxes
{
    public class CapInMemoryTransport : ITransport
    {
        private event Action<CapMessage> OnReceived;

        public Task<OperateResult> SendAsync(TransportMessage message)
        {
            var typeName = message.Headers["DotNetType"];
            var streamId = message.Headers["StreamId"];
            var type = Type.GetType(typeName);
            var json = Encoding.UTF8.GetString(message.Body);
            var evt = JsonConvert.DeserializeObject(json, type);
            OnReceived?.Invoke(new CapMessage(streamId, evt));
            return Task.FromResult(OperateResult.Success);
        }

        public BrokerAddress BrokerAddress { get; }

        public async Task Subscribe(Action<CapMessage> onReceived, CancellationToken cancellation)
        {
            var cancelling = new TaskCompletionSource<object>();
            cancellation.Register(() => cancelling.SetResult(null));
            OnReceived += onReceived;
            await cancelling.Task;
            OnReceived -= onReceived;
        }
    }
}
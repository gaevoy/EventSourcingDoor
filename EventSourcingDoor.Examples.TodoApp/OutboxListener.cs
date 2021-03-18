using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NEventStore;

namespace EventSourcingDoor.Examples.TodoApp
{
    public class OutboxListener : BackgroundService
    {
        private readonly IOutbox _outbox;
        private readonly ILogger _logger;

        public OutboxListener(IOutbox outbox, ILogger<OutboxListener> logger)
        {
            _outbox = outbox;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Started");
            await _outbox.Receive((evt, nesCommit) =>
            {
                EventsController.BroadcastEvent(evt).Wait();
                var checkpoint = ((ICommit) nesCommit).CheckpointToken;
                _logger.LogInformation("{Checkpoint}: {Event}", checkpoint, evt);
            }, stoppingToken);
            _logger.LogInformation("Stopped");
        }
    }
}
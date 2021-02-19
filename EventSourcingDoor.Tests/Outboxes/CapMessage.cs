namespace EventSourcingDoor.Tests.Outboxes
{
    public readonly struct CapMessage
    {
        public string StreamId { get; }
        public object Event { get; }

        public CapMessage(string streamId, object @event)
        {
            StreamId = streamId;
            Event = @event;
        }
    }
}
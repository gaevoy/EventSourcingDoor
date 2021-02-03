namespace EventSourcingDoor
{
    public interface IHasEventStream<TEventBase>
    {
        IAmEventStream<TEventBase> EventStream { get; }
    }
}
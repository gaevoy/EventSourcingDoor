namespace EventSourcingDoor
{
    public interface IHaveEventStream<TEventBase>
    {
        // TODO: Consider to rename to EventStream -> ChangeLog. 
        IAmEventStream<TEventBase> EventStream { get; }
    }
}
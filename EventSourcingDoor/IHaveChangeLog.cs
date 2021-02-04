namespace EventSourcingDoor
{
    public interface IHaveChangeLog<TEventBase>
    {
        IChangeLog<TEventBase> Changes { get; }
    }
}
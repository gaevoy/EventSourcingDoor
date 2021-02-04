namespace EventSourcingDoor
{
    public interface IHaveChangeLog
    {
        IChangeLog Changes { get; }
    }
}
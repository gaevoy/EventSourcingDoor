namespace EventSourcingDoor
{
    public interface IHaveVersion
    {
        long Version { get; set; }
    }
}
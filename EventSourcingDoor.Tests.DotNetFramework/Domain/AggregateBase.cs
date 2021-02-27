namespace EventSourcingDoor.Tests.Domain
{
    public abstract class AggregateBase<TAggregate, TEventBase> : IHaveChangeLog, IHaveStreamId
        where TAggregate : AggregateBase<TAggregate, TEventBase>
    {
        public abstract IChangeLog Changes { get; }
        public abstract string StreamId { get; }
        protected void ApplyChange(TEventBase evt) => Changes.Apply(evt);
    }
}
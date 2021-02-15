using System;

namespace EventSourcingDoor.Tests.Domain
{
    public class UserDeleted : IDomainEvent
    {
        public Guid Id { get; set; }
        public long Version { get; set; }
        public DateTime At { get; set; }
    }
}
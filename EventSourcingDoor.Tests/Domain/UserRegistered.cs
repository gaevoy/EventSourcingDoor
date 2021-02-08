using System;

namespace EventSourcingDoor.Tests.Domain
{
    public class UserRegistered : IDomainEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public long Version { get; set; }
    }
}
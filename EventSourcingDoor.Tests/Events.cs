using System;

namespace EventSourcingDoor.Tests
{
    public interface IDomainEvent
    {
    }

    public class UserRegistered : IDomainEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class UserNameChanged : IDomainEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
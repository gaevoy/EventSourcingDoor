using System;

namespace EventSourcingDoor.Tests
{
    public class UserAggregate : AggregateBase<UserAggregate, IDomainEvent>
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private static readonly ChangeLogDefinition<UserAggregate, IDomainEvent> CachedDefinition = ChangeLog
            .For<UserAggregate, IDomainEvent>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt));

        protected override ChangeLogDefinition<UserAggregate, IDomainEvent> Definition => CachedDefinition;

        public UserAggregate()
        {
        }

        public UserAggregate(Guid id, string name)
        {
            ApplyChange(new UserRegistered {Id = id, Name = name});
        }

        private void When(UserRegistered evt)
        {
            Id = evt.Id;
            Name = evt.Name;
        }

        public void Rename(string name)
        {
            ApplyChange(new UserNameChanged {Id = Id, Name = name});
        }

        private void When(UserNameChanged evt)
        {
            Name = evt.Name;
        }
    }
}
using System;

namespace EventSourcingDoor.Tests
{
    public class UserIsStream : AggregateBase<UserIsStream, IDomainEvent>
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private static readonly StreamDefinition<UserIsStream, IDomainEvent> CachedDefinition = StreamDefinition
            .For<UserIsStream, IDomainEvent>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt));

        protected override StreamDefinition<UserIsStream, IDomainEvent> Definition => CachedDefinition;

        public UserIsStream()
        {
        }

        public UserIsStream(Guid id, string name)
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
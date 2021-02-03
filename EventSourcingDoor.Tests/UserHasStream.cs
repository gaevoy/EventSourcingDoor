using System;

namespace EventSourcingDoor.Tests
{
    public class UserHasStream : IHasEventStream<IDomainEvent>
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private static readonly StreamDefinition<UserHasStream, IDomainEvent> Definition = StreamDefinition
            .For<UserHasStream, IDomainEvent>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt));

        public IAmEventStream<IDomainEvent> EventStream => _eventStream;
        private readonly EventStream<UserHasStream, IDomainEvent> _eventStream;

        public UserHasStream()
        {
            _eventStream = Definition.New(this);
        }

        public UserHasStream(Guid id, string name) : this()
        {
            _eventStream.ApplyChange(new UserRegistered {Id = id, Name = name});
        }

        private void When(UserRegistered evt)
        {
            Id = evt.Id;
            Name = evt.Name;
        }

        public void Rename(string name)
        {
            _eventStream.ApplyChange(new UserNameChanged {Id = Id, Name = name});
        }

        private void When(UserNameChanged evt)
        {
            Name = evt.Name;
        }
    }

 }
using System;

namespace EventSourcingDoor.Tests.Domain
{
    public class User : IHaveChangeLog, IHaveVersion, IHaveStreamId
    {
        public string StreamId => Id.ToString("N");
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public long Version { get; set; }
        public IChangeLog Changes => _changes;
        private readonly ChangeLog<User, IDomainEvent> _changes;

        private static readonly ChangeLogDefinition<User> Definition = ChangeLog
            .For<User>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt));

        public User()
        {
            _changes = Definition.New<IDomainEvent>(this);
        }

        public User(Guid id, string name) : this()
        {
            _changes.ApplyChange(new UserRegistered {Id = id, Name = name});
        }

        private void When(UserRegistered evt)
        {
            Id = evt.Id;
            Name = evt.Name;
        }

        public void Rename(string name)
        {
            _changes.ApplyChange(new UserNameChanged {Id = Id, Name = name});
        }

        private void When(UserNameChanged evt)
        {
            Name = evt.Name;
        }
    }
}
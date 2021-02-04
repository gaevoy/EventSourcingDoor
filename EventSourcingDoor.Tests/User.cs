using System;

namespace EventSourcingDoor.Tests
{
    public class User : IHaveChangeLog<IDomainEvent>, IHaveVersion
    {
        public Guid Id { get; private set; }
        public long Version { get; set; }
        public string Name { get; private set; }

        private static readonly ChangeLogDefinition<User, IDomainEvent> Definition = ChangeLog
            .For<User, IDomainEvent>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt));

        public IChangeLog<IDomainEvent> Changes => _changes;
        private readonly ChangeLog<User, IDomainEvent> _changes;

        public User()
        {
            _changes = Definition.New(this);
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
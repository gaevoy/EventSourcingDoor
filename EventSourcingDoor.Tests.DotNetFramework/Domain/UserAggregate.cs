using System;

namespace EventSourcingDoor.Tests.Domain
{
    public class UserAggregate : AggregateBase<UserAggregate, IDomainEvent>, IHaveVersion
    {
        public override string StreamId => Id.ToString("N");
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public long Version { get; set; }
        public DateTime? DeletedAt { get; set; }

        private static readonly ChangeLogDefinition<UserAggregate> Definition = ChangeLog
            .For<UserAggregate>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt))
            .On<UserDeleted>((self, evt) => self.When(evt));

        public override IChangeLog Changes { get; }

        public UserAggregate()
        {
            Changes = Definition.New<IDomainEvent>(this);
        }

        public UserAggregate(Guid id, string name) : this()
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

        public void Delete()
        {
            ApplyChange(new UserDeleted {Id = Id, At = DateTime.UtcNow});
        }

        private void When(UserDeleted evt)
        {
            DeletedAt = evt.At;
        }
    }
}
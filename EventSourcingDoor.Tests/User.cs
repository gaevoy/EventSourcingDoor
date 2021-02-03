using System;

namespace EventSourcingDoor.Tests
{
    public abstract class AggregateBase<TAggregate> : IHasEventStream<IDomainEvent>
        where TAggregate : AggregateBase<TAggregate>
    {
        protected abstract StreamDefinition<TAggregate, IDomainEvent> Definition { get; }
        public IAmEventStream<IDomainEvent> EventStream => Events;
        protected readonly EventStream<TAggregate, IDomainEvent> Events;

        protected AggregateBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Events = Definition.New((TAggregate) this);
        }
    }

    public class UserAggregate : AggregateBase<UserAggregate>
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private static readonly StreamDefinition<UserAggregate, IDomainEvent> CachedDefinition = StreamDefinition
            .For<UserAggregate, IDomainEvent>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt));

        protected override StreamDefinition<UserAggregate, IDomainEvent> Definition => CachedDefinition;

        public UserAggregate()
        {
        }

        public UserAggregate(Guid id, string name)
        {
            Events.ApplyChange(new UserRegistered {Id = id, Name = name});
        }

        private void When(UserRegistered evt)
        {
            Id = evt.Id;
            Name = evt.Name;
        }

        public void Rename(string name)
        {
            Events.ApplyChange(new UserNameChanged {Id = Id, Name = name});
        }

        private void When(UserNameChanged evt)
        {
            Name = evt.Name;
        }
    }

    public class User : IHasEventStream<IDomainEvent>
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private static readonly StreamDefinition<User, IDomainEvent> Definition = StreamDefinition
            .For<User, IDomainEvent>()
            .On<UserRegistered>((self, evt) => self.When(evt))
            .On<UserNameChanged>((self, evt) => self.When(evt));

        public IAmEventStream<IDomainEvent> EventStream => _eventStream;
        private readonly EventStream<User, IDomainEvent> _eventStream;

        public User()
        {
            _eventStream = Definition.New(this);
        }

        public User(Guid id, string name) : this()
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
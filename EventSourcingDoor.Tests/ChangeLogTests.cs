using System.Linq;
using EventSourcingDoor.Tests.Domain;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace EventSourcingDoor.Tests
{
    public class ChangeLogTests
    {
        private static Randomizer Random => TestContext.CurrentContext.Random;

        [Test]
        public void It_should_record_change_log_when_entity_is_created()
        {
            // Given
            var id = Random.NextGuid();
            var name = Random.GetString();

            // When
            var user = new User(id, name);

            // Then
            var changes = user.Changes.GetUncommittedChanges().ToList();
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0], Is.TypeOf<UserRegistered>());
            var evt = (UserRegistered) changes[0];
            Assert.That(evt.Id, Is.EqualTo(id));
            Assert.That(evt.Name, Is.EqualTo(name));
            Assert.That(evt.Version, Is.EqualTo(1));
            Assert.That(user.Version, Is.EqualTo(1));
        }

        [Test]
        public void It_should_record_change_log_when_entity_is_updated()
        {
            // Given
            var id = Random.NextGuid();
            var name = Random.GetString();
            var changedName = Random.GetString();
            var user = new User(id, name);
            user.Changes.MarkChangesAsCommitted();

            // When
            user.Rename(changedName);

            // Then
            var changes = user.Changes.GetUncommittedChanges().ToList();
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0], Is.TypeOf<UserNameChanged>());
            var evt = (UserNameChanged) changes[0];
            Assert.That(evt.Id, Is.EqualTo(id));
            Assert.That(evt.Name, Is.EqualTo(changedName));
            Assert.That(evt.Version, Is.EqualTo(2));
            Assert.That(user.Version, Is.EqualTo(2));
        }

        [Test]
        public void It_should_reply_change_log()
        {
            // Given
            var id = Random.NextGuid();
            var name = Random.GetString();
            var changedName = Random.GetString();
            var history = new IDomainEvent[]
            {
                new UserRegistered {Id = id, Name = name},
                new UserNameChanged {Name = changedName}
            };

            // When
            var user = new User();
            user.Changes.LoadFromHistory(history);

            // Then
            Assert.That(user.Changes.GetUncommittedChanges(), Is.Empty);
            Assert.That(user.Id, Is.EqualTo(id));
            Assert.That(user.Name, Is.EqualTo(changedName));
            Assert.That(user.Version, Is.EqualTo(2));
        }
    }
}
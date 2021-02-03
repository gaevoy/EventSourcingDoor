using System;
using NUnit.Framework;

namespace EventSourcingDoor.Tests
{
    public class Tests
    {
        [Test]
        public void Test1()
        {
            var user = new User(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.EventStream.GetUncommittedChanges();

            var user2 = new User();
            user2.EventStream.LoadFromHistory(user.EventStream.GetUncommittedChanges());
        }

        [Test]
        public void Test2()
        {
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.EventStream.GetUncommittedChanges();

            var user2 = new UserAggregate();
            user2.EventStream.LoadFromHistory(user.EventStream.GetUncommittedChanges());
        }
    }
}
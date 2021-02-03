using System;
using NUnit.Framework;

namespace EventSourcingDoor.Tests
{
    public class PlaygroundTests
    {
        [Test]
        public void Test1()
        {
            var user = new UserHasStream(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.EventStream.GetUncommittedChanges();

            var user2 = new UserHasStream();
            user2.EventStream.LoadFromHistory(user.EventStream.GetUncommittedChanges());
        }

        [Test]
        public void Test2()
        {
            var user = new UserIsStream(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.EventStream.GetUncommittedChanges();

            var user2 = new UserIsStream();
            user2.EventStream.LoadFromHistory(user.EventStream.GetUncommittedChanges());
        }
        
    }


}
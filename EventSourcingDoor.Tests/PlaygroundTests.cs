using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace EventSourcingDoor.Tests
{
    public class PlaygroundTests
    {
        [Test]
        public void Test1()
        {
            var user = new User(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.Changes.GetUncommittedChanges();

            var user2 = new User();
            user2.Changes.LoadFromHistory(user.Changes.GetUncommittedChanges());
        }

        [Test]
        public void Test2()
        {
            var user = new UserAggregate(Guid.NewGuid(), "Bond");
            user.Rename("James Bond");
            var events = user.Changes.GetUncommittedChanges();

            var user2 = new UserAggregate();
            user2.Changes.LoadFromHistory(user.Changes.GetUncommittedChanges());
        }

        [Test]
        public void Save()
        {
            var user = new UserAggregate(Guid.NewGuid(), "Bond");


            var user2 = new UserAggregate();
            user2.Changes.LoadFromHistory(user.Changes.GetUncommittedChanges());
        }
        
        public class TestDbContext : DbContextBase
        {
            public TestDbContext(string connectionString) : base(new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(connectionString).Options)
            {
            }

            public DbSet<UserAggregate> Users { get; set; }
        }

    }
}
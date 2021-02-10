using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;
using NEventStore;

namespace EventSourcingDoor.Tests.NEventStoreOutbox.EntityFramework
{
    public class TestDbContextWithOutbox : DbContextWithOutbox
    {
        public TestDbContextWithOutbox(string connectionString, IStoreEvents eventStore) : base(connectionString, eventStore)
        {
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
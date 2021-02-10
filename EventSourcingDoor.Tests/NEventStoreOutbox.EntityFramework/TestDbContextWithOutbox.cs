using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;
using NEventStore;

namespace EventSourcingDoor.Tests.NEventStoreOutbox.EntityFramework
{
    public class TestDbContextWithOutbox : DbContextWithOutbox
    {
        public TestDbContextWithOutbox(string connectionString, IStoreEvents eventStore)
            : base(connectionString, eventStore)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAggregate>()
                .Property(e => e.Version)
                .IsConcurrencyToken();
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
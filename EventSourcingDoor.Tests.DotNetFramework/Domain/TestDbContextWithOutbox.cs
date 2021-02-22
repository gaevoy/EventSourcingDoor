using System.Data.Common;
using System.Data.Entity;
using EventSourcingDoor.EntityFramework6;

namespace EventSourcingDoor.Tests.Domain
{
    public class TestDbContextWithOutbox : DbContextWithOutbox
    {
        public TestDbContextWithOutbox(string connectionString, IOutbox outbox)
            : base(connectionString, outbox)
        {
        }

        public TestDbContextWithOutbox(DbConnection connection, IOutbox outbox)
            : base(connection, true, outbox)
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
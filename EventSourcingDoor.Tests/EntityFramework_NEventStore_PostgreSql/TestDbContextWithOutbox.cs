using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Outboxes;

namespace EventSourcingDoor.Tests.EntityFramework_NEventStore_PostgreSql
{
    public class TestDbContextWithOutbox : DbContextWithOutbox
    {
        public TestDbContextWithOutbox(string connectionString, IOutbox outbox)
            : base(connectionString, outbox)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAggregate>()
                .Property(e => e.Version)
                .IsConcurrencyToken();
            modelBuilder.Types().Configure(c => c.ToTable(c.ClrType.Name, "public"));
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
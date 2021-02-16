using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;

namespace EventSourcingDoor.Tests.NEventStoreOutbox.EntityFramework.PostgreSql
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(string connectionString) : base(connectionString)
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
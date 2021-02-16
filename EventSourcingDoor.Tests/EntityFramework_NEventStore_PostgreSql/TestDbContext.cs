using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;

namespace EventSourcingDoor.Tests.EntityFramework_NEventStore_PostgreSql
{
    public class TestDbContext : EventSourcingDoor.Tests.Domain.TestDbContext
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
    }
}
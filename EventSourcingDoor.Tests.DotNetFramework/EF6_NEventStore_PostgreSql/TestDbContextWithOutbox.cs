using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;

namespace EventSourcingDoor.Tests.EF6_NEventStore_PostgreSql
{
    public class TestDbContextWithOutbox : EventSourcingDoor.Tests.Domain.TestDbContextWithOutbox
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
    }
}
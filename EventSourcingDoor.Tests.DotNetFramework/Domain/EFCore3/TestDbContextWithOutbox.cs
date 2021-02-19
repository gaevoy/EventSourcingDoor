using EventSourcingDoor.EntityFrameworkCore3;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.Tests.Domain.EFCore3
{
    public class TestDbContextWithOutbox : DbContextWithOutbox
    {
        public TestDbContextWithOutbox(DbContextOptions options, IOutbox outbox)
            : base(options, outbox)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAggregate>()
                .ToTable("UserAggregateEfCore3")
                .Property(e => e.Version)
                .IsConcurrencyToken();
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
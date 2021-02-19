using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.Tests.Domain.EFCore3
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
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
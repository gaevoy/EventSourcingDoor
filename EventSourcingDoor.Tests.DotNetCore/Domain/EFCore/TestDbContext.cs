using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.Tests.Domain.EFCore
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAggregate>()
                .ToTable("UserAggregateEfCore")
                .Property(e => e.Version)
                .IsConcurrencyToken();
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
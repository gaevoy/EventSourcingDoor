using System.Data.Entity;

namespace EventSourcingDoor.Tests.Domain
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(string connectionString) : base(connectionString)
        {
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;

namespace EventSourcingDoor.Tests.SqlStreamStoreUsage
{
    public class UsualEntityFramework : DbContext
    {
        public UsualEntityFramework(string connectionString) : base(connectionString)
        {
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
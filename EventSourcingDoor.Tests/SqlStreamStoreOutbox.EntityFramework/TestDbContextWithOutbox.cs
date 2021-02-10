using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;
using SqlStreamStore;

namespace EventSourcingDoor.Tests.SqlStreamStoreOutbox.EntityFramework
{
    public class TestDbContextWithOutbox : DbContextWithOutbox
    {
        public TestDbContextWithOutbox(string connectionString, IStreamStore eventStore) : base(connectionString, eventStore)
        {
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
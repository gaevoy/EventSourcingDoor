using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;
using SqlStreamStore;

namespace EventSourcingDoor.Tests.SqlStreamStoreUsage
{
    public class EventSourcedEntityFramework : EventSourcedDbContext
    {
        public EventSourcedEntityFramework(string connectionString, IStreamStore eventStore) : base(connectionString, eventStore)
        {
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
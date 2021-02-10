using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;
using SqlStreamStore;

namespace EventSourcingDoor.Tests.SqlStreamStoreUsage
{
    public class EventSourcedEntityFramework : EventSourcedDbContext
    {
        public EventSourcedEntityFramework(string connectionString, IStreamStore streamStore) : base(connectionString, streamStore)
        {
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
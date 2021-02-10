using System.Data.Entity;
using EventSourcingDoor.Tests.Domain;
using NEventStore;

namespace EventSourcingDoor.Tests.NEventStoreUsage
{
    public class EventSourcedEntityFramework : EventSourcedDbContext
    {
        public EventSourcedEntityFramework(string connectionString, IStoreEvents store) : base(connectionString, store)
        {
        }

        public DbSet<UserAggregate> Users { get; set; }
    }
}
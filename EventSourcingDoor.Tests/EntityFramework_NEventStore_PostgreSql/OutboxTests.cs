using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using EventSourcingDoor.Tests.Domain;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using NUnit.Framework;

#pragma warning disable 1998

namespace EventSourcingDoor.Tests.EntityFramework_NEventStore_PostgreSql
{
    [Parallelizable(ParallelScope.None)]
    public class OutboxTests : OutboxTestsBase
    {
        public string ConnectionString => "EventSourcingDoorConnectionString";
        private IStoreEvents _eventStore;

        [SetUp]
        public async Task EnsureSchemaInitialized()
        {
            // `receptionDelay` should include transaction timeout + clock drift. Otherwise, it may skip events during reception.
            var receptionDelay = TimeSpan.FromMilliseconds(3000);
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(ConnectionString)
                .WithDialect(new PostgreSqlDialect())
                .InitializeStorageEngine()
                .UsingJsonSerialization()
                .Build();
            eventStore.Advanced.Purge();
            _eventStore = eventStore;
            Outbox = new NEventStoreOutbox(eventStore, receptionDelay);
            var db = new TestDbContextWithOutbox(ConnectionString, Outbox);
            db.Database.CreateIfNotExists();
            // Warm-up
            for (int i = 0; i < 2; i++)
            {
                using var warmUpDb = NewDbContext();
                warmUpDb.Users.Add(new UserAggregate(Guid.NewGuid(), ""));
                await warmUpDb.SaveChangesAsync();
            }
        }

        protected override EventSourcingDoor.Tests.Domain.TestDbContextWithOutbox NewDbContext()
        {
            return new TestDbContextWithOutbox(ConnectionString, Outbox);
        }

        protected override async Task<List<IDomainEvent>> LoadChangeLog(string streamId)
        {
            return _eventStore.Advanced.GetFrom(Bucket.Default, streamId, 0, int.MaxValue)
                .SelectMany(e => e.Events)
                .Select(e => e.Body)
                .OfType<IDomainEvent>()
                .ToList();
        }

        protected override async Task<List<IDomainEvent>> LoadAllChangeLogs()
        {
            return _eventStore.Advanced.GetFrom(0)
                .SelectMany(e => e.Events)
                .Select(e => e.Body)
                .OfType<IDomainEvent>()
                .ToList();
        }
    }
}
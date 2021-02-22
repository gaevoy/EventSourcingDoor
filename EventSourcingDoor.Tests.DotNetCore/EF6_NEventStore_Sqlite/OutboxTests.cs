using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using EventSourcingDoor.Tests.Domain;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using NUnit.Framework;

#pragma warning disable 1998

namespace EventSourcingDoor.Tests.EF6_NEventStore_Sqlite
{
    [Parallelizable(ParallelScope.None)]
    [Ignore("WIP")]
    public class OutboxTests : OutboxTestsBase
    {
        public string ConnectionString => "Data Source=EventSourcingDoor.db;journal mode=WAL;cache=private;";
        private IStoreEvents _eventStore;

        [SetUp]
        public async Task EnsureSchemaInitialized()
        {
            // `receptionDelay` should include transaction timeout + clock drift. Otherwise, it may skip events during reception.
            var receptionDelay = TimeSpan.FromMilliseconds(3000);
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(SQLiteFactory.Instance, "Data Source=EventSourcingDoorOutbox.db;journal mode=WAL;cache=private;")
                .WithDialect(new SqliteDialect())
                .UsingJsonSerialization()
                .Build();
            _eventStore = eventStore;
            Outbox = new NEventStoreOutbox(eventStore, receptionDelay);
            using (var db = new TestDbContextWithOutbox(new SQLiteConnection(ConnectionString), Outbox))
            {
                db.Database.CreateIfNotExists();
            }

            eventStore.Advanced.Initialize();
            eventStore.Advanced.Purge();
        }

        protected override TestDbContextWithOutbox NewDbContext()
        {
            return new TestDbContextWithOutbox(new SQLiteConnection(ConnectionString), Outbox);
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
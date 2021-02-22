using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using EventSourcingDoor.Tests.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using NUnit.Framework;
using TestDbContextWithOutbox = EventSourcingDoor.Tests.Domain.EFCore.TestDbContextWithOutbox;

#pragma warning disable 1998

namespace EventSourcingDoor.Tests.EFCore_NEventStore_MsSql
{
    [Parallelizable(ParallelScope.None)]
    public class OutboxTests : OutboxTestsBase
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IStoreEvents _eventStore;

        [SetUp]
        public async Task EnsureSchemaInitialized()
        {
            // `receptionDelay` should include transaction timeout + clock drift. Otherwise, it may skip events during reception.
            var receptionDelay = TimeSpan.FromMilliseconds(3000);
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(SqlClientFactory.Instance, ConnectionString)
                .WithDialect(new MsSqlDialect())
                .UsingJsonSerialization()
                .Build();
            _eventStore = eventStore;
            Outbox = new NEventStoreOutbox(eventStore, receptionDelay);
            var options = new DbContextOptionsBuilder().UseSqlServer(ConnectionString).Options;
            var db = new TestDbContextWithOutbox(options, Outbox);
            try
            {
                _ = db.Users.FirstOrDefault();
            }
            catch (SqlException)
            {
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
            }

            eventStore.Advanced.Initialize();
            eventStore.Advanced.Purge();
        }

        protected override TestDbContextWithOutbox NewDbContext()
        {
            var options = new DbContextOptionsBuilder().UseSqlServer(ConnectionString).Options;
            return new TestDbContextWithOutbox(options, Outbox);
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
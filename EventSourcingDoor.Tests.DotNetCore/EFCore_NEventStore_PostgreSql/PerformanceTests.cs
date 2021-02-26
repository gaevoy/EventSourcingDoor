using System;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using Microsoft.EntityFrameworkCore;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using Npgsql;
using NUnit.Framework;
using TestDbContext = EventSourcingDoor.Tests.Domain.EFCore.TestDbContext;
using TestDbContextWithOutbox = EventSourcingDoor.Tests.Domain.EFCore.TestDbContextWithOutbox;

namespace EventSourcingDoor.Tests.EFCore_NEventStore_PostgreSql
{
    [Parallelizable(ParallelScope.None), Explicit]
    public class PerformanceTests : PerformanceTestsBase
    {
        public string ConnectionString => "Server=localhost;Port=5432;Database=EventSourcingDoor;User ID=postgres;Password=sa123;";

        private IOutbox _outbox;

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(NpgsqlFactory.Instance, ConnectionString)
                .WithDialect(new PostgreSqlDialect())
                .UsingJsonSerialization()
                .Build();
            _outbox = new NEventStoreOutbox(eventStore, TimeSpan.Zero);
            var options = new DbContextOptionsBuilder().UseNpgsql(ConnectionString).Options;
            var db = new TestDbContextWithOutbox(options, _outbox);
            try
            {
                _ = db.Users.FirstOrDefault();
            }
            catch (PostgresException)
            {
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
            }

            eventStore.Advanced.Initialize();
            await WarmUp();
        }

        protected override TestDbContextWithOutbox NewDbContextWithOutbox()
        {
            var options = new DbContextOptionsBuilder().UseNpgsql(ConnectionString).Options;
            return new TestDbContextWithOutbox(options, _outbox);
        }

        protected override TestDbContext NewDbContext()
        {
            var options = new DbContextOptionsBuilder().UseNpgsql(ConnectionString).Options;
            return new TestDbContext(options);
        }
    }
}
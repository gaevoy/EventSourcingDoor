using System;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NEventStore;
using NEventStore.Serialization.Json;
using NUnit.Framework;
using TestDbContext = EventSourcingDoor.Tests.Domain.EFCore.TestDbContext;
using TestDbContextWithOutbox = EventSourcingDoor.Tests.Domain.EFCore.TestDbContextWithOutbox;

namespace EventSourcingDoor.Tests.EFCore_NEventStore_Sqlite
{
    [Parallelizable(ParallelScope.None), Explicit]
    public class PerformanceTests : PerformanceTestsBase
    {
        public string ConnectionString => "Data Source=EventSourcingDoor.db;";
        private IOutbox _outbox;

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(SqliteFactory.Instance, ConnectionString)
                .WithDialect(new FixedSqliteDialect())
                .UsingJsonSerialization()
                .Build();
            _outbox = new NEventStoreOutbox(eventStore, TimeSpan.Zero);
            var options = new DbContextOptionsBuilder().UseSqlite(ConnectionString).Options;
            var db = new TestDbContextWithOutbox(options, _outbox);
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
            await WarmUp();
        }

        protected override TestDbContextWithOutbox NewDbContextWithOutbox()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlite(ConnectionString)
                .ConfigureWarnings(x =>
                    x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning))
                .Options;
            return new TestDbContextWithOutbox(options, _outbox);
        }

        protected override TestDbContext NewDbContext()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlite(ConnectionString)
                .ConfigureWarnings(x =>
                    x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning))
                .Options;
            return new TestDbContext(options);
        }
    }
}
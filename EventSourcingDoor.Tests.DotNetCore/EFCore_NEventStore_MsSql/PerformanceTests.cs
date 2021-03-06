using System;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using NUnit.Framework;
using TestDbContext = EventSourcingDoor.Tests.Domain.EFCore.TestDbContext;
using TestDbContextWithOutbox = EventSourcingDoor.Tests.Domain.EFCore.TestDbContextWithOutbox;

namespace EventSourcingDoor.Tests.EFCore_NEventStore_MsSql
{
    [Parallelizable(ParallelScope.None), Explicit]
    public class PerformanceTests : PerformanceTestsBase
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IOutbox _outbox;

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(SqlClientFactory.Instance, ConnectionString)
                .WithDialect(new MsSqlDialect())
                .UsingJsonSerialization()
                .Build();
            _outbox = new NEventStoreOutbox(eventStore, TimeSpan.Zero);
            var options = new DbContextOptionsBuilder().UseSqlServer(ConnectionString).Options;
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
            var options = new DbContextOptionsBuilder().UseSqlServer(ConnectionString).Options;
            return new TestDbContextWithOutbox(options, _outbox);
        }

        protected override TestDbContext NewDbContext()
        {
            var options = new DbContextOptionsBuilder().UseSqlServer(ConnectionString).Options;
            return new TestDbContext(options);
        }
    }
}
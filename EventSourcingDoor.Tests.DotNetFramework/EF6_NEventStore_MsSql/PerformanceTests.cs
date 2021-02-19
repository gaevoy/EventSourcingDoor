using System;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using EventSourcingDoor.Tests.Domain;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using NUnit.Framework;

namespace EventSourcingDoor.Tests.EF6_NEventStore_MsSql
{
    [Parallelizable(ParallelScope.None), Explicit]
    public class PerformanceTests : PerformanceTestsBase
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IOutbox _outbox;

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            _outbox = new NEventStoreOutbox(Wireup.Init()
                .UsingSqlPersistence(null, "System.Data.SqlClient", ConnectionString)
                .WithDialect(new MsSqlDialect())
                .InitializeStorageEngine()
                .UsingJsonSerialization()
                .Build(), TimeSpan.Zero);
            var db = new TestDbContextWithOutbox(ConnectionString, _outbox);
            db.Database.CreateIfNotExists();
            await WarmUp();
        }

        protected override TestDbContextWithOutbox NewDbContextWithOutbox()
        {
            return new TestDbContextWithOutbox(ConnectionString, _outbox);
        }

        protected override TestDbContext NewDbContext()
        {
            return new TestDbContext(ConnectionString);
        }
    }
}
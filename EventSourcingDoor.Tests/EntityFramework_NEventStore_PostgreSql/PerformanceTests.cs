using System;
using System.Threading.Tasks;
using EventSourcingDoor.Tests.Outboxes;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using NUnit.Framework;

namespace EventSourcingDoor.Tests.EntityFramework_NEventStore_PostgreSql
{
    [Parallelizable(ParallelScope.None), Explicit]
    public class PerformanceTests : PerformanceTestsBase
    {
        public string ConnectionString => "EventSourcingDoorConnectionString";
        private IOutbox _outbox;

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            _outbox = new NEventStoreOutbox(Wireup.Init()
                .UsingSqlPersistence(ConnectionString)
                .WithDialect(new PostgreSqlDialect())
                .InitializeStorageEngine()
                .UsingJsonSerialization()
                .Build(), TimeSpan.Zero);
            var db = new TestDbContextWithOutbox(ConnectionString, _outbox);
            db.Database.CreateIfNotExists();
            await WarmUp();
        }

        protected override EventSourcingDoor.Tests.Domain.TestDbContextWithOutbox NewDbContextWithOutbox()
        {
            return new TestDbContextWithOutbox(ConnectionString, _outbox);
        }

        protected override EventSourcingDoor.Tests.Domain.TestDbContext NewDbContext()
        {
            return new TestDbContext(ConnectionString);
        }
    }
}
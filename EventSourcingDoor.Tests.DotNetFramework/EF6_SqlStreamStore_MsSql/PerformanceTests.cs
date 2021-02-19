using System;
using System.Threading.Tasks;
using EventSourcingDoor.SqlStreamStore;
using EventSourcingDoor.Tests.Domain;
using NUnit.Framework;
using SqlStreamStore;

namespace EventSourcingDoor.Tests.EF6_SqlStreamStore_MsSql
{
    [Parallelizable(ParallelScope.None), Explicit]
    public class PerformanceTests : PerformanceTestsBase
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IOutbox _outbox;

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            var eventStore = new MsSqlStreamStoreV3(new MsSqlStreamStoreV3Settings(ConnectionString));
            await eventStore.CreateSchemaIfNotExists();
            _outbox = new SqlStreamStoreOutbox(eventStore, TimeSpan.Zero);
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
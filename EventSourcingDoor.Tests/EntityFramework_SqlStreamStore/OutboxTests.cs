using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Outboxes;
using Newtonsoft.Json;
using NUnit.Framework;
using SqlStreamStore;

namespace EventSourcingDoor.Tests.EntityFramework_SqlStreamStore
{
    [Parallelizable(ParallelScope.None)]
    public class OutboxTests : OutboxTestsBase
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IStreamStore _eventStore;

        [SetUp]
        public async Task EnsureSchemaInitialized()
        {
            // `receptionDelay` should include transaction timeout + clock drift. Otherwise, it may skip events during reception.
            var receptionDelay = TimeSpan.FromMilliseconds(3000);
            var eventStore = new MsSqlStreamStoreV3(new MsSqlStreamStoreV3Settings(ConnectionString));
            await eventStore.DropAll();
            await eventStore.CreateSchemaIfNotExists();
            _eventStore = eventStore;
            Outbox = new SqlStreamStoreOutbox(eventStore, receptionDelay);
            var db = new TestDbContextWithOutbox(ConnectionString, Outbox);
            db.Database.CreateIfNotExists();
        }

        protected override TestDbContextWithOutbox NewDbContext()
        {
            return new TestDbContextWithOutbox(ConnectionString, Outbox);
        }

        protected override async Task<List<IDomainEvent>> LoadChangeLog(string streamId)
        {
            var changeLog = new List<IDomainEvent>();
            var page = await _eventStore.ReadStreamForwards(streamId, 0, int.MaxValue);
            foreach (var message in page.Messages)
            {
                var json = await message.GetJsonData();
                var evt = (IDomainEvent) JsonConvert.DeserializeObject(json, Type.GetType(message.Type));
                changeLog.Add(evt);
            }

            return changeLog;
        }

        protected override async Task<List<IDomainEvent>> LoadAllChangeLogs()
        {
            var changeLog = new List<IDomainEvent>();
            var page = await _eventStore.ReadAllForwards(0, int.MaxValue);
            foreach (var message in page.Messages)
            {
                var json = await message.GetJsonData();
                var evt = (IDomainEvent) JsonConvert.DeserializeObject(json, Type.GetType(message.Type));
                changeLog.Add(evt);
            }

            return changeLog;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Processor;
using DotNetCore.CAP.Transport;
using EventSourcingDoor.Tests.Cap;
using EventSourcingDoor.Tests.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace EventSourcingDoor.Tests.EF6_Cap
{
    [Parallelizable(ParallelScope.None)]
    [Ignore("Cap is broken due lack of `TransactionScope` support.")]
    public class OutboxTests : OutboxTestsBase
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        public readonly List<CapMessage> Events = new List<CapMessage>();

        [SetUp]
        public void EnsureSchemaInitialized()
        {
            // Cap is broken due to the line `var fourMinAgo = DateTime.Now.AddMinutes(-4).ToString("O");` in `SqlServerDataStorage`
            var services = new ServiceCollection();
            services.AddLogging(e =>
            {
                e.ClearProviders();
                e.AddConsole();
            });
            services.AddSingleton<ITransport, CapInMemoryTransport>();
            services.AddSingleton<IProcessor, MessageNeedToRetryProcessor>();
            // services.AddSingleton<IDispatcher, CapNullDispatcher>(); // Uncomment if you want to receive messages from database, otherwise in-memory queue is enabled, which does not support `TransactionScope`.
            services.AddCap(e =>
            {
                e.FailedRetryInterval = 1;
                e.UseSqlServer(ConnectionString);
            });
            var container = services.BuildServiceProvider();
            _ = container.GetService<IBootstrapper>().BootstrapAsync(default);
            var capPublisher = container.GetService<ICapPublisher>();
            var capTransport = (CapInMemoryTransport) container.GetService<ITransport>();
            Outbox = new CapOutbox(capPublisher, capTransport);
            var db = new TestDbContextWithOutbox(ConnectionString, Outbox);
            db.Database.CreateIfNotExists();

            lock (Events) Events.Clear();
            _ = capTransport.Subscribe(m =>
            {
                lock (Events) Events.Add(m);
            }, CancellationToken.None);
        }

        protected override TestDbContextWithOutbox NewDbContext()
        {
            return new TestDbContextWithOutbox(ConnectionString, Outbox);
        }

        protected override async Task<List<IDomainEvent>> LoadChangeLog(string streamId)
        {
            await Task.Delay(2000);
            lock (Events)
                return Events.Where(e => e.StreamId == streamId).Select(e => e.Event).OfType<IDomainEvent>().ToList();
        }

        protected override async Task<List<IDomainEvent>> LoadAllChangeLogs()
        {
            await Task.Delay(2000);
            lock (Events)
                return Events.Select(e => e.Event).OfType<IDomainEvent>().ToList();
        }
    }
}
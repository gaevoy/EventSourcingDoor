using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcingDoor.NEventStore;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using NUnit.Framework;
using TestDbContext = EventSourcingDoor.Tests.Domain.EFCore3.TestDbContext;
using TestDbContextWithOutbox = EventSourcingDoor.Tests.Domain.EFCore3.TestDbContextWithOutbox;

namespace EventSourcingDoor.Tests.EFCore3_NEventStore_MsSql
{
    [Parallelizable(ParallelScope.None), Explicit]
    public class PerformanceTests
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private readonly SemaphoreSlim _throttler = new SemaphoreSlim( /*degreeOfParallelism:*/ 10);
        private IOutbox _outbox;

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            var eventStore = Wireup.Init()
                .UsingSqlPersistence(null, "System.Data.SqlClient", ConnectionString)
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

        protected TestDbContextWithOutbox NewDbContextWithOutbox()
        {
            var options = new DbContextOptionsBuilder().UseSqlServer(ConnectionString).Options;
            return new TestDbContextWithOutbox(options, _outbox);
        }

        protected TestDbContext NewDbContext()
        {
            var options = new DbContextOptionsBuilder().UseSqlServer(ConnectionString).Options;
            return new TestDbContext(options);
        }

        [Test, Repeat(5)]
        public async Task EntityFrameworkWithOutboxAsync()
        {
            var timings = new List<long>();
            var globalTiming = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, 1_000).Select(_ => InsertSomeUser()).ToList();
            await Task.WhenAll(tasks);
            globalTiming.Stop();
            Console.WriteLine($"total: {globalTiming.ElapsedMilliseconds}, local: {timings.Average()}");

            async Task InsertSomeUser()
            {
                await Task.Yield();
                using var _ = await _throttler.Throttle();
                var id = Guid.NewGuid();
                var name = id.ToString();
                var localTiming = Stopwatch.StartNew();
                using (var db = NewDbContextWithOutbox())
                {
                    db.Users.Add(new UserAggregate(id, name));
                    await db.SaveChangesAsync();
                }

                localTiming.Stop();
                lock (timings)
                    timings.Add(localTiming.ElapsedMilliseconds);
            }
        }

        [Test, Repeat(5)]
        public async Task EntityFrameworkWithOutboxSync()
        {
            var timings = new List<long>();
            var globalTiming = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, 1_000).Select(_ => InsertSomeUser()).ToList();
            await Task.WhenAll(tasks);
            globalTiming.Stop();
            Console.WriteLine($"total: {globalTiming.ElapsedMilliseconds}, local: {timings.Average()}");

            async Task InsertSomeUser()
            {
                await Task.Yield();
                using var _ = await _throttler.Throttle();
                var id = Guid.NewGuid();
                var name = id.ToString();
                var localTiming = Stopwatch.StartNew();
                using (var db = NewDbContextWithOutbox())
                {
                    db.Users.Add(new UserAggregate(id, name));
                    db.SaveChanges();
                }

                localTiming.Stop();
                lock (timings)
                    timings.Add(localTiming.ElapsedMilliseconds);
            }
        }

        [Test, Repeat(5)]
        public async Task UsualEntityFramework()
        {
            var timings = new List<long>();
            var globalTiming = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, 1_000).Select(_ => InsertSomeUser()).ToList();
            await Task.WhenAll(tasks);
            globalTiming.Stop();
            Console.WriteLine($"total: {globalTiming.ElapsedMilliseconds}, local: {timings.Average()}");

            async Task InsertSomeUser()
            {
                await Task.Yield();
                using var _ = await _throttler.Throttle();
                var id = Guid.NewGuid();
                var name = id.ToString();
                var localTiming = Stopwatch.StartNew();
                using (var db = NewDbContext())
                {
                    db.Users.Add(new UserAggregate(id, name));
                    await db.SaveChangesAsync();
                }

                localTiming.Stop();
                lock (timings)
                    timings.Add(localTiming.ElapsedMilliseconds);
            }
        }

        protected async Task WarmUp()
        {
            Stopwatch.StartNew().Stop();
            for (var i = 0; i < 1_000; i++)
            {
                using var db = NewDbContextWithOutbox();
                db.Users.Add(new UserAggregate(Guid.NewGuid(), Guid.NewGuid().ToString()));
                await db.SaveChangesAsync();
            }

            for (var i = 0; i < 1_000; i++)
            {
                using var db = NewDbContext();
                db.Users.Add(new UserAggregate(Guid.NewGuid(), Guid.NewGuid().ToString()));
                await db.SaveChangesAsync();
            }
        }
    }
}
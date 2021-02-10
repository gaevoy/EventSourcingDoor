using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Utils;
using NUnit.Framework;
using SqlStreamStore;

namespace EventSourcingDoor.Tests.SqlStreamStoreOutbox.EntityFramework
{
    [Parallelizable(ParallelScope.None)]
    public class PerformanceTests
    {
        public string ConnectionString => "server=localhost;database=EventSourcingDoor;UID=sa;PWD=sa123";
        private IStreamStore _eventStore;
        private readonly SemaphoreSlim _throttler = new SemaphoreSlim( /*degreeOfParallelism:*/ 10);

        [SetUp]
        public async Task InitializeAndWarmUp()
        {
            var eventStore = new MsSqlStreamStoreV3(new MsSqlStreamStoreV3Settings(ConnectionString));
            await eventStore.CreateSchemaIfNotExists();
            _eventStore = eventStore;
            var db = new TestDbContextWithOutbox(ConnectionString, _eventStore);
            db.Database.CreateIfNotExists();
            await WarmUpEntityFrameworkWithOutbox();
            await WarmUpUsualEntityFramework();
            Stopwatch.StartNew().Stop();

            async Task WarmUpEntityFrameworkWithOutbox()
            {
                for (int i = 0; i < 1_000; i++)
                {
                    using var db = new TestDbContextWithOutbox(ConnectionString, _eventStore);
                    db.Users.Add(new UserAggregate(Guid.NewGuid(), Guid.NewGuid().ToString()));
                    await db.SaveChangesAsync();
                }
            }

            async Task WarmUpUsualEntityFramework()
            {
                for (int i = 0; i < 1_000; i++)
                {
                    using var db = new TestDbContext(ConnectionString);
                    db.Users.Add(new UserAggregate(Guid.NewGuid(), Guid.NewGuid().ToString()));
                    await db.SaveChangesAsync();
                }
            }
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
                using (var db = new TestDbContextWithOutbox(ConnectionString, _eventStore))
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
                using (var db = new TestDbContext(ConnectionString))
                {
                    db.Users.Add(new UserAggregate(id, name));
                    await db.SaveChangesAsync();
                }

                localTiming.Stop();
                lock (timings)
                    timings.Add(localTiming.ElapsedMilliseconds);
            }
        }
    }
}
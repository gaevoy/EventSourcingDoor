using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcingDoor.Tests.Domain;
using EventSourcingDoor.Tests.Domain.EFCore;
using EventSourcingDoor.Tests.Utils;
using NUnit.Framework;

namespace EventSourcingDoor.Tests
{
    public abstract class PerformanceTestsBase
    {
        private readonly SemaphoreSlim _throttler = new SemaphoreSlim( /*degreeOfParallelism:*/ 10);

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
            for (var i = 0; i < 100; i++)
            {
                using var db = NewDbContextWithOutbox();
                db.Users.Add(new UserAggregate(Guid.NewGuid(), Guid.NewGuid().ToString()));
                await db.SaveChangesAsync();
            }

            for (var i = 0; i < 100; i++)
            {
                using var db = NewDbContext();
                db.Users.Add(new UserAggregate(Guid.NewGuid(), Guid.NewGuid().ToString()));
                await db.SaveChangesAsync();
            }
        }

        protected abstract TestDbContextWithOutbox NewDbContextWithOutbox();
        protected abstract TestDbContext NewDbContext();
    }
}
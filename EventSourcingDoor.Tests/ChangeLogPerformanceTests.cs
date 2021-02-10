using System;
using System.Diagnostics;
using System.Linq;
using EventSourcingDoor.Tests.Domain;
using NUnit.Framework;

namespace EventSourcingDoor.Tests
{
    [Parallelizable(ParallelScope.None)]
    public class ChangeLogPerformanceTests
    {
        [SetUp]
        public void WarmUp()
        {
            var history = Enumerable
                .Range(0, 10_000)
                .Select(i => new UserNameChanged {Name = i.ToString()})
                .OfType<IDomainEvent>()
                .ToList();
            history.Insert(0, new UserRegistered {Id = Guid.Empty, Name = "_"});
            new User().Changes.LoadFromHistory(history);
        }

        [Test, Repeat(10)]
        public void It_should_reply_history()
        {
            // Given
            var count = 10_000_000;
            var history = Enumerable
                .Range(0, count)
                .Select(i => new UserNameChanged {Name = i.ToString()})
                .OfType<IDomainEvent>()
                .ToList();
            history.Insert(0, new UserRegistered {Id = Guid.Empty, Name = "_"});
            var user = new User();

            // When
            var stopwatch = Stopwatch.StartNew();
            user.Changes.LoadFromHistory(history);
            stopwatch.Stop();

            // Then
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }
    }
}
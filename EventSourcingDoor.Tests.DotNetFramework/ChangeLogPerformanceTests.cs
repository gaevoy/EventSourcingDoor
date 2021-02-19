using System;
using System.Diagnostics;
using System.Linq;
using EventSourcingDoor.Tests.Domain;
using NUnit.Framework;

namespace EventSourcingDoor.Tests
{
    [Parallelizable(ParallelScope.None), Explicit]
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

        [Test, Repeat(10)]
        public void It_should_create_user_entity()
        {
            // Given
            var count = 1_000_000;

            // When
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var user = new User(Guid.Empty, "User");
                user.Rename("2");
                user.Rename("3");
                user.Rename("4");
                user.Rename("5");
            }

            stopwatch.Stop();

            // Then
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        [Test, Repeat(5)]
        public void It_should_reply_history_when_there_is_20_different_event_types()
        {
            var count = 10_000_000;
            var state = new TestState();
            var changes = ChangeLog
                .For<TestState>()
                .On<TestEvent01>(Handle)
                .On<TestEvent02>(Handle)
                .On<TestEvent03>(Handle)
                .On<TestEvent04>(Handle)
                .On<TestEvent05>(Handle)
                .On<TestEvent06>(Handle)
                .On<TestEvent07>(Handle)
                .On<TestEvent08>(Handle)
                .On<TestEvent09>(Handle)
                .On<TestEvent10>(Handle)
                .On<TestEvent11>(Handle)
                .On<TestEvent12>(Handle)
                .On<TestEvent13>(Handle)
                .On<TestEvent14>(Handle)
                .On<TestEvent15>(Handle)
                .On<TestEvent16>(Handle)
                .On<TestEvent17>(Handle)
                .On<TestEvent18>(Handle)
                .On<TestEvent19>(Handle)
                .On<TestEvent20>(Handle)
                .New(state);
            var historyFor01 = Enumerable
                .Range(0, count)
                .Select(_ => new TestEvent01 {CurrentValue = Guid.NewGuid()})
                .ToList();
            var historyFor20 = Enumerable
                .Range(0, count)
                .Select(_ => new TestEvent20 {CurrentValue = Guid.NewGuid()})
                .ToList();

            var stopwatch = Stopwatch.StartNew();
            changes.LoadFromHistory(historyFor01);
            stopwatch.Stop();
            Console.WriteLine($"TestEvent01: {stopwatch.ElapsedMilliseconds}");

            stopwatch = Stopwatch.StartNew();
            changes.LoadFromHistory(historyFor20);
            stopwatch.Stop();
            Console.WriteLine($"TestEvent20: {stopwatch.ElapsedMilliseconds}");
        }

        private static void Handle(TestState state, TestEventBase evt)
        {
            state.CurrentValue = evt.CurrentValue;
        }

        public class TestState : IHaveStreamId
        {
            public Guid CurrentValue { get; set; }
            public string StreamId { get; set; }
        }

        // @formatter:off
        public class TestEventBase { public Guid CurrentValue { get; set; } }
        public class TestEvent01 : TestEventBase { }
        public class TestEvent02 : TestEventBase { }
        public class TestEvent03 : TestEventBase { }
        public class TestEvent04 : TestEventBase { }
        public class TestEvent05 : TestEventBase { }
        public class TestEvent06 : TestEventBase { }
        public class TestEvent07 : TestEventBase { }
        public class TestEvent08 : TestEventBase { }
        public class TestEvent09 : TestEventBase { }
        public class TestEvent10 : TestEventBase { }
        public class TestEvent11 : TestEventBase { }
        public class TestEvent12 : TestEventBase { }
        public class TestEvent13 : TestEventBase { }
        public class TestEvent14 : TestEventBase { }
        public class TestEvent15 : TestEventBase { }
        public class TestEvent16 : TestEventBase { }
        public class TestEvent17 : TestEventBase { }
        public class TestEvent18 : TestEventBase { }
        public class TestEvent19 : TestEventBase { }
        public class TestEvent20 : TestEventBase { }
        // @formatter:on
    }
}
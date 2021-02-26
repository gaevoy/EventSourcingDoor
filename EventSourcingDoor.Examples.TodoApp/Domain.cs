using System;
using System.Collections.Generic;
using System.Linq;
using EventSourcingDoor.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.Examples.TodoApp
{
    public class TodoDbContext : DbContextWithOutbox
    {
        public TodoDbContext(DbContextOptions options, IOutbox outbox) : base(options, outbox)
        {
        }

        public DbSet<Goal> Goals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Goal>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<TodoTask>().Property(p => p.Id).ValueGeneratedNever();
        }
    }

    public class Goal : IHaveChangeLog
    {
        public Guid Id { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Description { get; private set; }
        public bool IsAchieved { get; private set; }
        public List<TodoTask> Tasks { get; private set; } = new();
        public IChangeLog Changes { get; }

        public static ChangeLogDefinition<Goal> Definition = ChangeLog.For<Goal>()
            .On<GoalSet>((self, evt) => self.When(evt))
            .On<GoalRefined>((self, evt) => self.When(evt))
            .On<TaskAdded>((self, evt) => self.When(evt))
            .On<GoalAchievementChanged>((self, evt) => self.When(evt))
            .And(TodoTask.Definition);

        protected Goal()
        {
            Changes = Definition.New(this);
        }

        public Goal(Guid id, string description) : this()
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            ApplyChange(new GoalSet(id, description));
        }

        private void When(GoalSet evt)
        {
            Id = evt.GoalId;
            Description = evt.Description;
            CreatedAt = evt.At;
        }

        public void Refine(string description)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (Description != description)
                ApplyChange(new GoalRefined(Id, description));
        }

        private void When(GoalRefined evt)
        {
            Description = evt.Description;
        }

        public void AddTask(Guid id, string description)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            ApplyChange(new TaskAdded(Id, id, description));
            UpdateAchievement();
        }

        private void When(TaskAdded evt)
        {
            Tasks.Add(new TodoTask(this, evt.TaskId, evt.At, evt.Description));
        }

        public void UpdateAchievement()
        {
            var isAchieved = Tasks.All(e => e.IsFinished);
            if (IsAchieved != isAchieved)
                ApplyChange(new GoalAchievementChanged(Id, isAchieved));
        }

        private void When(GoalAchievementChanged evt)
        {
            IsAchieved = evt.IsAchieved;
        }

        public TodoTask GetTask(Guid id)
        {
            return Tasks.Find(e => e.Id == id);
        }

        private void ApplyChange(DomainEvent evt) => Changes.Apply(evt);
    }

    public class TodoTask
    {
        public Goal Goal { get; private set; }
        public Guid GoalId { get; private set; }
        public Guid Id { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Description { get; private set; }
        public bool IsFinished { get; private set; }

        public static ChangeLogDefinition<Goal> Definition = ChangeLog.For<Goal>()
            .On<TaskChanged>(When)
            .On<TaskFinished>(When);

        protected TodoTask()
        {
        }

        internal TodoTask(Goal goal, Guid id, DateTimeOffset createdAt, string description) : this()
        {
            Goal = goal;
            GoalId = goal.Id;
            Id = id;
            CreatedAt = createdAt;
            Description = description;
        }

        public void Change(string description)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (Description != description)
                ApplyChange(new TaskChanged(Goal.Id, Id, description));
        }

        private static void When(Goal goal, TaskChanged evt)
        {
            var task = goal.GetTask(evt.TaskId);
            task.Description = evt.Description;
        }

        public void Finish()
        {
            if (IsFinished == false)
                ApplyChange(new TaskFinished(Goal.Id, Id));
            Goal.UpdateAchievement();
        }

        private static void When(Goal goal, TaskFinished evt)
        {
            var task = goal.GetTask(evt.TaskId);
            task.IsFinished = true;
        }

        private void ApplyChange(DomainEvent evt) => Goal.Changes.Apply(evt);
    }

    public record GoalSet(Guid GoalId, string Description) : DomainEvent;

    public record GoalRefined(Guid GoalId, string Description) : DomainEvent;

    public record GoalAchievementChanged(Guid GoalId, bool IsAchieved) : DomainEvent;

    public record TaskAdded(Guid GoalId, Guid TaskId, string Description) : DomainEvent;

    public record TaskChanged(Guid GoalId, Guid TaskId, string Description) : DomainEvent;

    public record TaskFinished(Guid GoalId, Guid TaskId) : DomainEvent;

    public abstract record DomainEvent(DateTimeOffset At)
    {
        protected DomainEvent() : this(DateTimeOffset.Now)
        {
        }
    }
}
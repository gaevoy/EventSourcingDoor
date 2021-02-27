using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.Examples.TodoApp
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Goal> Goals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Goal>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<TodoTask>().Property(p => p.Id).ValueGeneratedNever();
        }
    }

    public class Goal
    {
        public Guid Id { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Description { get; private set; }
        public bool IsAchieved { get; private set; }
        public List<TodoTask> Tasks { get; private set; } = new();

        protected Goal()
        {
        }

        public Goal(Guid id, string description) : this()
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            Id = id;
            Description = description;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public void Refine(string description)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            Description = description;
        }

        public void AddTask(Guid id, string description)
        {
            var task = new TodoTask(this, id, description);
            Tasks.Add(task);
            UpdateAchievement();
        }

        public void UpdateAchievement()
        {
            IsAchieved = Tasks.All(e => e.IsFinished);
        }

        public TodoTask GetTask(Guid id)
        {
            return Tasks.Find(e => e.Id == id);
        }
    }

    public class TodoTask
    {
        public Goal Goal { get; private set; }
        public Guid GoalId { get; private set; }
        public Guid Id { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Description { get; private set; }
        public bool IsFinished { get; private set; }

        protected TodoTask()
        {
        }

        internal TodoTask(Goal goal, Guid id, string description) : this()
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            Goal = goal;
            GoalId = goal.Id;
            Id = id;
            CreatedAt = DateTimeOffset.UtcNow;
            Description = description;
        }

        public void Change(string description)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            Description = description;
        }

        public void Finish()
        {
            IsFinished = true;
            Goal.UpdateAchievement();
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.TodoListExample.Domain
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<TodoList> TodoLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoList>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<TodoTask>().Property(p => p.Id).ValueGeneratedNever();
        }
    }

    public class TodoList
    {
        public Guid Id { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Name { get; private set; }
        public List<TodoTask> Tasks { get; private set; } = new();

        protected TodoList()
        {
        }

        public TodoList(Guid id, string name)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public void Rename(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void AddTask(Guid id, string description)
        {
            var task = new TodoTask(this, id, description);
            Tasks.Add(task);
        }
    }

    public class TodoTask
    {
        public TodoList List { get; private set; }
        public Guid ListId { get; private set; }
        public Guid Id { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public string Description { get; private set; }
        public bool IsFinished { get; private set; }

        protected TodoTask()
        {
        }

        public TodoTask(TodoList list, Guid id, string description)
        {
            List = list;
            ListId = list.Id;
            Id = id;
            CreatedAt = DateTimeOffset.UtcNow;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public void ChangeDescription(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public void Finish()
        {
            IsFinished = true;
        }
    }
}
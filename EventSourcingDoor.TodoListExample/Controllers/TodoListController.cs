using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcingDoor.TodoListExample.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.TodoListExample.Controllers
{
    [ApiController]
    [Route("api/todolists")]
    public class TodoListController : ControllerBase
    {
        private readonly TodoDbContext _db;

        public TodoListController(TodoDbContext dbContext)
        {
            _db = dbContext;
        }

        [HttpPost]
        public async Task<Guid> CreateList(string name)
        {
            var listId = Guid.NewGuid();
            var list = new TodoList(listId, name);
            await _db.TodoLists.AddAsync(list);
            await _db.SaveChangesAsync();
            return listId;
        }

        [HttpGet]
        public async Task<List<TodoList>> GetLists()
        {
            return await _db.TodoLists.OrderBy(e => e.CreatedAt).ToListAsync();
        }

        [HttpPut("{listId}")]
        public async Task RenameList(Guid listId, string name)
        {
            var list = await _db.TodoLists.FindAsync(listId);
            list.Rename(name);
            await _db.SaveChangesAsync();
        }

        [HttpPost("{listId}/tasks")]
        public async Task<Guid> AddTask(Guid listId, string description)
        {
            var list = await _db.TodoLists.FindAsync(listId);
            var taskId = Guid.NewGuid();
            list.AddTask(taskId, description);
            await _db.SaveChangesAsync();
            return taskId;
        }

        [HttpGet("{listId}/tasks")]
        public async Task<List<TodoTask>> GetTasks(Guid listId)
        {
            var tasks = await _db.TodoLists
                .Where(e => e.Id == listId)
                .SelectMany(e => e.Tasks)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();
            return tasks;
        }

        [HttpPut("{listId}/tasks/{taskId}")]
        public async Task ChangeTaskDescription(Guid listId, Guid taskId, string description)
        {
            var list = await _db.TodoLists.Where(e => e.Id == listId).Include(e => e.Tasks).FirstAsync();
            var task = list.Tasks.First(e => e.Id == taskId);
            task.ChangeDescription(description);
            await _db.SaveChangesAsync();
        }

        [HttpDelete("{listId}/tasks/{taskId}")]
        public async Task FinishTask(Guid listId, Guid taskId)
        {
            var list = await _db.TodoLists.Where(e => e.Id == listId).Include(e => e.Tasks).FirstAsync();
            var task = list.Tasks.First(e => e.Id == taskId);
            task.Finish();
            await _db.SaveChangesAsync();
        }
    }
}
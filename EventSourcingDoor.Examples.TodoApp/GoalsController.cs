using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventSourcingDoor.Examples.TodoApp
{
    [ApiController]
    [Route("api/goals")]
    public class GoalsController : ControllerBase
    {
        private readonly TodoDbContext _db;

        public GoalsController(TodoDbContext dbContext)
        {
            _db = dbContext;
        }

        [HttpPost]
        public async Task<Guid> SetGoal(string description)
        {
            var goalId = Guid.NewGuid();
            var goal = new Goal(goalId, description);
            await _db.Goals.AddAsync(goal);
            await _db.SaveChangesAsync();
            return goalId;
        }

        [HttpGet]
        public async Task<List<Goal>> GetGoals()
        {
            return await _db.Goals.OrderBy(e => e.CreatedAt).ToListAsync();
        }

        [HttpPut("{goalId}")]
        public async Task RefineGoal(Guid goalId, string description)
        {
            var goal = await _db.Goals.FindAsync(goalId);
            goal.Refine(description);
            await _db.SaveChangesAsync();
        }

        [HttpPost("{goalId}/tasks")]
        public async Task<Guid> AddTask(Guid goalId, string description)
        {
            var goal = await _db.Goals.FindAsync(goalId);
            var taskId = Guid.NewGuid();
            goal.AddTask(taskId, description);
            await _db.SaveChangesAsync();
            return taskId;
        }

        [HttpGet("{goalId}/tasks")]
        public async Task<List<TodoTask>> GetTasks(Guid goalId)
        {
            var tasks = await _db.Goals
                .Where(e => e.Id == goalId)
                .SelectMany(e => e.Tasks)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();
            return tasks;
        }

        [HttpPut("{goalId}/tasks/{taskId}")]
        public async Task ChangeTask(Guid goalId, Guid taskId, string description)
        {
            var goal = await _db.Goals.Where(e => e.Id == goalId).Include(e => e.Tasks).FirstAsync();
            var task = goal.GetTask(taskId);
            task.Change(description);
            await _db.SaveChangesAsync();
        }

        [HttpDelete("{goalId}/tasks/{taskId}")]
        public async Task FinishTask(Guid goalId, Guid taskId)
        {
            var goal = await _db.Goals.Where(e => e.Id == goalId).Include(e => e.Tasks).FirstAsync();
            var task = goal.GetTask(taskId);
            task.Finish();
            await _db.SaveChangesAsync();
        }
    }
}
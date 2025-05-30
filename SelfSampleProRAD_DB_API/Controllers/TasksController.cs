using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfSampleProRAD_DB_API.Models;
using SelfSampleProRAD_DB_API.DTOs;
using SelfSampleProRAD_DB_API.Data;
using Microsoft.AspNetCore.Authorization;
using SelfSampleProRAD_DB_API.Services;

namespace SelfSampleProRAD_DB_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Base authorization for all endpoints
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TasksController(AppDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Assign a new task
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "RequireManager")] // Only managers can assign tasks
        public async Task<ActionResult<EmployeeTasks>> AssignTask([FromBody] AssignTaskDTO request)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                Guid employeeId = JwtService.ExtractEmployeeIDClaimsFromJWT(this.User, "employeeId"); // Extract employee ID from JWT claims
                if (employeeId == Guid.Empty) return BadRequest("Invalid Employee ID.");

                var task = new Tasks
                {
                    TaskName = request.TaskName,
                    Status = 'P'
                };
                var taskResponse = _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                var employeeTask = new EmployeeTasks
                {
                    TaskId = taskResponse.Entity.TaskId,
                    AssignedToId = Guid.Parse(request.AssignedToId),
                    AssignedById = employeeId
                };
                var employeeTaskResponse = _context.EmployeeTasks.Add(employeeTask);
                await _context.SaveChangesAsync();
                transaction.Commit();
                return Ok(new { Data = employeeTask, Message = "Task Successfully assigned" });
            } catch (Exception ex) { 
                return StatusCode(500, "Error: " + ex.Message); 
            }
        }

        private static string GetStatusText(char status)
        {
            if (status == 'P') return "Pending";
            if (status == 'S') return "Started";
            if (status == 'C') return "Completed";
            return "Unknown";
        }

        /// <summary>
        /// View tasks assigned to an employee
        /// </summary>
        [HttpGet("assigned-to/{taskTo}")]
        [Authorize(Policy = "RequireEmployee")] // Any employee can view tasks assigned to them
        public async Task<ActionResult<List<TaskViewToResponseDTO>>> ViewTasksFor(Guid taskTo)
        {
            try
            {
                Guid employeeId = JwtService.ExtractEmployeeIDClaimsFromJWT(this.User, "employeeId"); // Extract employee ID from JWT claims
                if (employeeId == Guid.Empty) return BadRequest("Invalid Employee ID.");

                var tasks = await _context.EmployeeTasks
                    .Where(t => t.AssignedToId == employeeId && t.Tasks.Status !='C')
                    .Select(t => new TaskViewToResponseDTO
                    {
                        TaskId = t.TaskId,
                        FirstName = t.AssignedBy.FirstName,
                        LastName = t.AssignedBy.LastName,
                        TaskName = t.Tasks.TaskName,
                        Status = GetStatusText(t.Tasks.Status)
                    })
                    .ToListAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        /// <summary>
        /// View tasks assigned by an employee
        /// </summary>
        [HttpGet("assigned-by/{taskBy}")]
        [Authorize(Policy = "RequireManager")] // Only managers can view tasks they assigned
        public async Task<ActionResult<List<TaskViewByResponseDTO>>> ViewTasksBy(Guid taskBy)
        {
            try
            {
                Guid employeeId = JwtService.ExtractEmployeeIDClaimsFromJWT(this.User, "employeeId"); // Extract employee ID from JWT claims
                if (employeeId == Guid.Empty) return BadRequest("Invalid Employee ID.");

                var tasks = await _context.EmployeeTasks
                    .Where(t => t.AssignedById == employeeId)
                    .Select(t => new TaskViewByResponseDTO
                    {
                        FullName = $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}",
                        TaskName = t.Tasks.TaskName,
                        Status = GetStatusText(t.Tasks.Status)
                    })
                    .ToListAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        /// <summary>
        /// Start working on a task
        /// </summary>
        [HttpPut("{taskId}/start")]
        [Authorize(Policy = "RequireDeveloper")] // Only developers can start working on tasks
        public async Task<ActionResult> startWorking(Guid taskId)
        {
            var task = await _context.Tasks
                .Where(t => t.TaskId == taskId).FirstOrDefaultAsync();
            if (task == null) return NotFound("Task not found.");
            if (task.Status == 'C') return BadRequest("Task is already completed.");
            task.Status = 'S';
            try
            {
                _context.Update(task);
                await _context.SaveChangesAsync();
                return Ok("Task started.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Complete a task
        /// </summary>
        [HttpPut("{taskId}/complete")]
        [Authorize(Policy = "RequireDeveloper")] // Only developers can complete tasks
        public async Task<ActionResult> submitWork(Guid taskId)
        {
            var task = await _context.Tasks
                .Where(t => t.TaskId == taskId).FirstOrDefaultAsync();
            if (task == null) return NotFound("Task not found.");
            if (task.Status == 'C') return BadRequest("Task is already completed.");
            task.Status = 'C';
            try
            {
                _context.Update(task);
                await _context.SaveChangesAsync();
                return Ok("Task completed.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }
    }
}

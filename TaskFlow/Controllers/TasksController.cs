using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Models;
using System.Security.Claims;

namespace TaskFlow.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TaskController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            var userId = GetUserId();

            var column = await _context.Columns
                .Include(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == dto.ColumnId);

            if (column == null)
                return NotFound();

            var role = await GetUserRole(column.BoardId);

            if (role == null)
                return Forbid();

            var lastPosition = await _context.Tasks
                .Where(t => t.ColumnId == dto.ColumnId)
                .MaxAsync(t => (int?)t.Position) ?? -1;

            var task = new TaskItem
            {
                ColumnId = dto.ColumnId,
                Title = dto.Title,
                Description = dto.Description,
                Position = lastPosition + 1
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.Title,
                task.Description,
                task.Position,
                task.ColumnId
            });

        }


        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveTask(int id, MoveTaskDto dto)
        {
            var userId = GetUserId();

            var task = await _context.Tasks
                .Include(t => t.Column)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var role = await GetUserRole(task.Column.BoardId);

            if (role == null)
                return Forbid();

            var oldColumnId = task.ColumnId;
            var oldPosition = task.Position;

            var oldColumnTasks = await _context.Tasks
                .Where(t => t.ColumnId == oldColumnId && t.Position > oldPosition)
                .ToListAsync();

            foreach (var t in oldColumnTasks)
                t.Position--;

            var newColumnTasks = await _context.Tasks
                .Where(t => t.ColumnId == dto.NewColumnId && t.Position >= dto.NewPosition)
                .ToListAsync();

            foreach (var t in newColumnTasks)
                t.Position++;

            task.ColumnId = dto.NewColumnId;
            task.Position = dto.NewPosition;

            await _context.SaveChangesAsync();

            return Ok(task);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();

            var task = await _context.Tasks
                .Include(t => t.Column)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var isMember = await _context.BoardMembers
                .AnyAsync(bm => bm.BoardId == task.Column.BoardId && bm.UserId == userId);

            if (!isMember)
                return Forbid();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok();
        }
        private async Task<BoardRole?> GetUserRole(int boardId)
        {
            var userId = GetUserId();

            var member = await _context.BoardMembers
                .FirstOrDefaultAsync(bm => bm.BoardId == boardId && bm.UserId == userId);

            return member?.Role;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto dto)
        {
            var userId = GetUserId();

            var task = await _context.Tasks
                .Include(t => t.Column)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var role = await GetUserRole(task.Column.BoardId);

            if (role == null)
                return Forbid();

            task.Title = dto.Title;
            task.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.Title,
                task.Description,
                task.Position,
                task.ColumnId
            });
        }


    }
}

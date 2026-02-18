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
    public class ColumnController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ColumnController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [HttpPost]
        public async Task<IActionResult> CreateColumn(CreateColumnDto dto)
        {
            var userId = GetUserId();

            var role = await GetUserRole(dto.BoardId);

            if (role == null || role == BoardRole.Member)
                return Forbid();

            var lastPosition = await _context.Columns
                .Where(c => c.BoardId == dto.BoardId)
                .MaxAsync(c => (int?)c.Position) ?? -1;

            var column = new TaskColumn
            {
                BoardId = dto.BoardId,
                Name = dto.Name,
                Position = lastPosition + 1
            };

            _context.Columns.Add(column);
            await _context.SaveChangesAsync();

            return Ok(column);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColumn(int id)
        {
            var userId = GetUserId();

            var column = await _context.Columns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (column == null)
                return NotFound();

            var role = await GetUserRole(column.BoardId);

            if (role == null || role == BoardRole.Member)
                return Forbid();

            var position = column.Position;

            _context.Columns.Remove(column);

            var columnsToShift = await _context.Columns
                .Where(c => c.BoardId == column.BoardId && c.Position > position)
                .ToListAsync();

            foreach (var col in columnsToShift)
                col.Position--;

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
        public async Task<IActionResult> UpdateColumn(int id, UpdateColumnDto dto)
        {
            var userId = GetUserId();

            var column = await _context.Columns
                .Include(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (column == null)
                return NotFound();

            var role = await GetUserRole(column.BoardId);

            if (role == null || role == BoardRole.Member)
                return Forbid();

            column.Name = dto.Name;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                column.Id,
                column.Name,
                column.Position,
                column.BoardId
            });

        }

        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveColumn(int id, ReorderColumnDto dto)
        {
            var userId = GetUserId();

            var column = await _context.Columns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (column == null)
                return NotFound();

            var role = await GetUserRole(column.BoardId);

            if (role == null || role == BoardRole.Member)
                return Forbid();

            var oldPosition = column.Position;
            var newPosition = dto.NewPosition;

            if (oldPosition == newPosition)
                return Ok(column);

            var columns = await _context.Columns
                .Where(c => c.BoardId == column.BoardId)
                .ToListAsync();

            if (newPosition > oldPosition)
            {
                foreach (var col in columns
                    .Where(c => c.Position > oldPosition && c.Position <= newPosition))
                {
                    col.Position--;
                }
            }
            else
            {
                foreach (var col in columns
                    .Where(c => c.Position >= newPosition && c.Position < oldPosition))
                {
                    col.Position++;
                }
            }

            column.Position = newPosition;

            await _context.SaveChangesAsync();

            return Ok(column);
        }


    }
}

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

            var member = await _context.BoardMembers
                .FirstOrDefaultAsync(bm => bm.BoardId == dto.BoardId && bm.UserId == userId);

            if (member == null)
                return Forbid();

            if (member.Role == BoardRole.Member)
                return Forbid();

            var column = new TaskColumn
            {
                BoardId = dto.BoardId,
                Name = dto.Name,
                Position = dto.Position
            };

            _context.Columns.Add(column);
            await _context.SaveChangesAsync();

            return Ok(column);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            var role = await GetUserRole(id);

            if (role != BoardRole.Owner)
                return Forbid();

            var board = await _context.Boards.FindAsync(id);

            if (board == null)
                return NotFound();

            _context.Boards.Remove(board);
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
    }
}

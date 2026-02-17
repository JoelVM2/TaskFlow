using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Models;

namespace TaskFlow.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BoardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BoardController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyBoards()
        {
            var userId = GetUserId();

            var boards = await _context.BoardMembers
                .Where(bm => bm.UserId == userId)
                .Select(bm => new
                {
                    bm.Board.Id,
                    bm.Board.Name,
                    bm.Board.JoinCode
                })
                .ToListAsync();

            return Ok(boards);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBoard(int id)
        {
            var userId = GetUserId();

            var isMember = await _context.BoardMembers
                .AnyAsync(bm => bm.BoardId == id && bm.UserId == userId);

            if (!isMember)
                return Forbid();

            var board = await _context.Boards
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    Columns = b.Columns
                        .OrderBy(c => c.Position)
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Position,
                            Tasks = c.Tasks
                                .OrderBy(t => t.Position)
                                .Select(t => new
                                {
                                    t.Id,
                                    t.Title,
                                    t.Description,
                                    t.Position
                                })
                        })
                })
                .FirstOrDefaultAsync();

            if (board == null)
                return NotFound();

            return Ok(board);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinBoard(JoinBoardDto dto)
        {
            var userId = GetUserId();

            // Buscar board por JoinCode
            var board = await _context.Boards
                .FirstOrDefaultAsync(b => b.JoinCode == dto.JoinCode);

            if (board == null)
                return NotFound("Invalid join code");

            // Verificar si ya es miembro
            var alreadyMember = await _context.BoardMembers
                .AnyAsync(bm => bm.BoardId == board.Id && bm.UserId == userId);

            if (alreadyMember)
                return BadRequest("Already a member of this board");

            var boardMember = new BoardMember
            {
                BoardId = board.Id,
                UserId = userId,
                Role = BoardRole.Member
            };

            _context.BoardMembers.Add(boardMember);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                board.Id,
                board.Name,
                board.JoinCode
            });
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

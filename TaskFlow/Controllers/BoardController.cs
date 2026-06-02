using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Models;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Controlador de tableros: listado, detalle, creación, edición, borrado
    /// y unión por código. Requiere autenticación.
    /// </summary>
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

        /// <summary>Obtiene el id del usuario autenticado a partir del token JWT.</summary>
        /// <returns>Identificador del usuario.</returns>
        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        /// <summary>Lista los tableros de los que el usuario autenticado es miembro.</summary>
        /// <returns>200 con el listado (id, nombre y código) de sus tableros.</returns>
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

        /// <summary>
        /// Obtiene un tablero completo (columnas y tareas ordenadas) e indica si el usuario es propietario.
        /// </summary>
        /// <param name="id">Identificador del tablero.</param>
        /// <returns>200 con el tablero; 403 si no es miembro; 404 si no existe.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBoard(int id)
        {
            var userId = GetUserId();

            var membership = await _context.BoardMembers
                .FirstOrDefaultAsync(bm => bm.BoardId == id && bm.UserId == userId);

            if (membership == null)
                return Forbid();

            var board = await _context.Boards
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.JoinCode,
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

            return Ok(new
            {
                board.Id,
                board.Name,
                board.JoinCode,
                IsOwner = membership.Role == BoardRole.Owner,
                board.Columns
            });
        }

        /// <summary>Une al usuario autenticado a un tablero mediante su código de unión.</summary>
        /// <param name="dto">Código de unión del tablero.</param>
        /// <returns>200 con el tablero; 404 si el código no existe; 400 si ya es miembro.</returns>
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

        /// <summary>Crea un tablero nuevo y deja al usuario autenticado como Owner.</summary>
        /// <param name="dto">Nombre del tablero.</param>
        /// <returns>200 con el tablero creado (id, nombre y código).</returns>
        [HttpPost]
        public async Task<IActionResult> CreateBoard(CreateBoardDto dto)
        {
            var userId = GetUserId();

            var joinCode = GenerateJoinCode();

            var board = new Board
            {
                Name = dto.Name,
                JoinCode = joinCode,
                OwnerId = userId
            };

            _context.Boards.Add(board);
            await _context.SaveChangesAsync();

            var boardMember = new BoardMember
            {
                BoardId = board.Id,
                UserId = userId,
                Role = BoardRole.Owner
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

        /// <summary>Elimina un tablero. Solo el propietario (Owner) puede hacerlo.</summary>
        /// <param name="id">Identificador del tablero.</param>
        /// <returns>200 si se elimina; 403 si no es Owner; 404 si no existe.</returns>
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


        /// <summary>Devuelve el rol del usuario autenticado en un tablero (o null si no es miembro).</summary>
        /// <param name="boardId">Identificador del tablero.</param>
        /// <returns>El rol (Owner/Member) o null.</returns>
        private async Task<BoardRole?> GetUserRole(int boardId)
        {
            var userId = GetUserId();

            var member = await _context.BoardMembers
                .FirstOrDefaultAsync(bm => bm.BoardId == boardId && bm.UserId == userId);

            return member?.Role;
        }

        /// <summary>Genera un código de unión aleatorio de 6 caracteres alfanuméricos.</summary>
        /// <returns>El código de unión.</returns>
        private string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>Renombra un tablero. Solo el propietario (Owner) puede hacerlo.</summary>
        /// <param name="id">Identificador del tablero.</param>
        /// <param name="dto">Nuevo nombre del tablero.</param>
        /// <returns>200 con el tablero actualizado; 403 si no es Owner; 404 si no existe.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBoard(int id, UpdateBoardDto dto)
        {
            var role = await GetUserRole(id);

            if (role != BoardRole.Owner)
                return Forbid();

            var board = await _context.Boards.FindAsync(id);

            if (board == null)
                return NotFound();

            board.Name = dto.Name;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                board.Id,
                board.Name,
                board.JoinCode
            });
        }

    }
}

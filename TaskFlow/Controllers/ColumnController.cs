using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Models;
using System.Security.Claims;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Controlador de columnas: creación, edición, borrado y reordenación
    /// dentro de un tablero. Requiere autenticación.
    /// </summary>
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

        /// <summary>Obtiene el id del usuario autenticado a partir del token JWT.</summary>
        /// <returns>Identificador del usuario.</returns>
        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        /// <summary>Crea una columna al final del tablero. Solo el propietario (Owner) puede hacerlo.</summary>
        /// <param name="dto">Id del tablero y nombre de la columna.</param>
        /// <returns>200 con la columna creada; 403 si no es Owner del tablero.</returns>
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


        /// <summary>
        /// Elimina una columna y recoloca las posiciones del resto. Solo el propietario puede hacerlo.
        /// </summary>
        /// <param name="id">Identificador de la columna.</param>
        /// <returns>200 si se elimina; 404 si no existe; 403 si no es Owner del tablero.</returns>
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

        /// <summary>Renombra una columna. Solo el propietario (Owner) puede hacerlo.</summary>
        /// <param name="id">Identificador de la columna.</param>
        /// <param name="dto">Nuevo nombre.</param>
        /// <returns>200 con la columna actualizada; 404 si no existe; 403 si no es Owner.</returns>
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

        /// <summary>
        /// Reordena una columna a una nueva posición, recolocando el resto. Solo el propietario puede hacerlo.
        /// </summary>
        /// <param name="id">Identificador de la columna.</param>
        /// <param name="dto">Nueva posición (base 0).</param>
        /// <returns>200 con la columna; 404 si no existe; 403 si no es Owner del tablero.</returns>
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
                return Ok(new { column.Id, column.Name, column.Position, column.BoardId });

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

            return Ok(new { column.Id, column.Name, column.Position, column.BoardId });
        }


    }
}

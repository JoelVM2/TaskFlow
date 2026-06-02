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
    /// Controlador de tareas. Gestiona la creación, edición, borrado
    /// y movimiento de tareas entre columnas. Requiere autenticación.
    /// </summary>
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

        /// <summary>Obtiene el id del usuario autenticado a partir del token JWT.</summary>
        /// <returns>Identificador del usuario.</returns>
        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        /// <summary>Crea una tarea al final de la columna indicada.</summary>
        /// <param name="dto">Título, descripción y id de la columna destino.</param>
        /// <returns>200 con la tarea creada; 404 si la columna no existe; 403 si no es miembro del tablero.</returns>
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


        /// <summary>
        /// Mueve una tarea a otra columna y/o posición, recolocando el resto de tareas afectadas.
        /// </summary>
        /// <param name="id">Identificador de la tarea a mover.</param>
        /// <param name="dto">Columna destino y nueva posición (base 0).</param>
        /// <returns>200 con la tarea movida; 404 si no existe; 403 si no es miembro del tablero.</returns>
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

            return Ok(new
            {
                task.Id,
                task.Title,
                task.Description,
                task.Position,
                task.ColumnId
            });
        }


        /// <summary>Elimina una tarea. Cualquier miembro del tablero puede hacerlo.</summary>
        /// <param name="id">Identificador de la tarea.</param>
        /// <returns>200 si se elimina; 404 si no existe; 403 si no es miembro del tablero.</returns>
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

        /// <summary>Edita el título y la descripción de una tarea.</summary>
        /// <param name="id">Identificador de la tarea.</param>
        /// <param name="dto">Nuevo título y descripción.</param>
        /// <returns>200 con la tarea actualizada; 404 si no existe; 403 si no es miembro del tablero.</returns>
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

namespace TaskFlow.DTOs
{
    /// <summary>Columna destino y nueva posición (base 0) de una tarea.</summary>
    public class MoveTaskDto
    {
        public int NewColumnId { get; set; }
        public int NewPosition { get; set; }
    }
}

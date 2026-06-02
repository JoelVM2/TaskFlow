using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    /// <summary>Nuevos título y descripción de una tarea.</summary>
    public class UpdateTaskDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public string Description { get; set; }
    }
}

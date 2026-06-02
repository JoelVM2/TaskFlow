using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    /// <summary>Nuevo nombre de una columna.</summary>
    public class UpdateColumnDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}

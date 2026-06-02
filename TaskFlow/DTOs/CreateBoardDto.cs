using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    /// <summary>Datos para crear un tablero.</summary>
    public class CreateBoardDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}

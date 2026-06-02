using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    /// <summary>Código para unirse a un tablero.</summary>
    public class JoinBoardDto
    {
        [Required]
        [MaxLength(10)]
        public string JoinCode { get; set; }
    }
}

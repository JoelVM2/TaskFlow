using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    public class JoinBoardDto
    {
        [Required]
        [MaxLength(10)]
        public string JoinCode { get; set; }
    }
}

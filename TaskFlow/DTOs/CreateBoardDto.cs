using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    public class CreateBoardDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}

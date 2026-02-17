using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    public class CreateColumnDto
    {
        [Required]
        public int BoardId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        public int Position { get; set; }
    }
}

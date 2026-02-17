using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    public class CreateTaskDto
    {
        [Required]
        public int ColumnId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public string Description { get; set; }

        public int Position { get; set; }
    }
}

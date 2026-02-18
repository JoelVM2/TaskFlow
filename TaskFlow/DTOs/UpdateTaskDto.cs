using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    public class UpdateTaskDto
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public string Description { get; set; }
    }
}

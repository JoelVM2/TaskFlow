using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    public class UpdateColumnDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}

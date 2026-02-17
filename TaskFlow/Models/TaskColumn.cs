using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models
{
    public class TaskColumn
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        public int Position { get; set; }

        public int BoardId { get; set; }

        public Board Board { get; set; }
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}

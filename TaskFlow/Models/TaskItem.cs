using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public string Description { get; set; }

        public int ColumnId { get; set; }

        public int? AssignedTo { get; set; }

        public int Position { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public TaskColumn Column { get; set; }
        public User AssignedUser { get; set; }
    }
}

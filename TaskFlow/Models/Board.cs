using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models
{
    public class Board
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string JoinCode { get; set; }

        public int OwnerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public User Owner { get; set; }
        public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
        public ICollection<TaskColumn> Columns { get; set; } = new List<TaskColumn>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        
        public ICollection<Board> OwnedBoards { get; set; } = new List<Board>();
        public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
        public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    }
}

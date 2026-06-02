using TaskFlow.Models;

/// <summary>Relación entre un usuario y un tablero, con su rol. Clave compuesta (UserId + BoardId).</summary>
public class BoardMember
{
    public int UserId { get; set; }
    public int BoardId { get; set; }

    public BoardRole Role { get; set; } = BoardRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; }
    public Board Board { get; set; }
}

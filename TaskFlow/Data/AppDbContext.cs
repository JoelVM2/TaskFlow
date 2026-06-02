using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;

namespace TaskFlow.Data
{
    /// <summary>
    /// Contexto de Entity Framework Core. Expone las tablas (DbSet) y configura
    /// las relaciones, la clave compuesta de BoardMember y las reglas de borrado.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardMember> BoardMembers { get; set; }
        public DbSet<TaskColumn> Columns { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }

        /// <summary>Configura las relaciones entre entidades y sus reglas de borrado.</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Clave compuesta de BoardMember (UserId + BoardId)
            modelBuilder.Entity<BoardMember>()
                .HasKey(bm => new { bm.UserId, bm.BoardId });

            modelBuilder.Entity<BoardMember>()
            .Property(bm => bm.Role)
            .HasConversion<int>();

            // Usuario - Tableros en propiedad (1:N)
            modelBuilder.Entity<Board>()
                .HasOne(b => b.Owner)
                .WithMany(u => u.OwnedBoards)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // BoardMember - User (N:1)
            modelBuilder.Entity<BoardMember>()
                .HasOne(bm => bm.User)
                .WithMany(u => u.BoardMembers)
                .HasForeignKey(bm => bm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // BoardMember - Board (N:1)
            modelBuilder.Entity<BoardMember>()
                .HasOne(bm => bm.Board)
                .WithMany(b => b.BoardMembers)
                .HasForeignKey(bm => bm.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Board - Columns (1:N)
            modelBuilder.Entity<TaskColumn>()
                .HasOne(c => c.Board)
                .WithMany(b => b.Columns)
                .HasForeignKey(c => c.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Column - Tasks (1:N)
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Column)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            // Task - Assigned User (N:1)
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

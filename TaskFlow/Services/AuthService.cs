using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskFlow.Data;
using TaskFlow.DTOs;
using TaskFlow.Models;

namespace TaskFlow.Services
{
    /// <summary>
    /// Servicio de autenticación: registro (con tablero inicial), login,
    /// hashing de contraseñas y generación del token JWT.
    /// </summary>
    public class AuthService
    {
        private readonly AppDbContext _context;

        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Registra un usuario dentro de una transacción y le crea un tablero por defecto
        /// ("{usuario}'s Board") con las columnas To Do / In Progress / Done, dejándolo como Owner.
        /// </summary>
        /// <param name="dto">Datos de registro (usuario, email, contraseña).</param>
        /// <returns><c>true</c> si se registra; <c>false</c> si el email ya existe o falla la transacción.</returns>
        public async Task<bool> RegisterAsync(RegisterDto dto)
        {
            // Verificar si ya existe email
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Crear usuario
                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = HashPassword(dto.Password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Crear board
                var board = new Board
                {
                    Name = $"{dto.Username}'s Board",
                    OwnerId = user.Id,
                    JoinCode = GenerateJoinCode()
                };

                _context.Boards.Add(board);
                await _context.SaveChangesAsync();

                // Insertar como Owner en BoardMembers
                var boardMember = new BoardMember
                {
                    UserId = user.Id,
                    BoardId = board.Id,
                    Role = BoardRole.Owner,
                };

                _context.BoardMembers.Add(boardMember);

                // Crear columnas por defecto
                var columns = new List<TaskColumn>
                {
                    new TaskColumn { Name = "To Do", Position = 0, BoardId = board.Id },
                    new TaskColumn { Name = "In Progress", Position = 1, BoardId = board.Id },
                    new TaskColumn { Name = "Done", Position = 2, BoardId = board.Id }
                };

                _context.Columns.AddRange(columns);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>Genera un código de unión aleatorio de 6 caracteres alfanuméricos.</summary>
        /// <returns>El código de unión.</returns>
        private string GenerateJoinCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>Calcula el hash de una contraseña (SHA-256, en Base64).</summary>
        /// <param name="password">Contraseña en texto plano.</param>
        /// <returns>El hash en Base64.</returns>
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>Valida las credenciales y, si son correctas, genera un token JWT.</summary>
        /// <param name="dto">Email y contraseña.</param>
        /// <returns>El token JWT, o <c>null</c> si las credenciales no son válidas.</returns>
        public async Task<string?> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return null;

            var hashedPassword = HashPassword(dto.Password);

            if (user.PasswordHash != hashedPassword)
                return null;

            return GenerateJwtToken(user);
        }

        /// <summary>Genera un token JWT firmado con los claims del usuario (id, email, nombre).</summary>
        /// <param name="user">Usuario para el que se genera el token.</param>
        /// <returns>El token JWT serializado.</returns>
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"])
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Username)
    };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(jwtSettings["ExpiresInMinutes"])
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace TaskFlow.DTOs
{
    /// <summary>Credenciales para iniciar sesión.</summary>
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}

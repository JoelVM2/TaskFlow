using Microsoft.AspNetCore.Mvc;
using TaskFlow.DTOs;
using TaskFlow.Services;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Controlador de autenticación: registro de usuarios e inicio de sesión (JWT).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registra un nuevo usuario y le crea un tablero por defecto con columnas iniciales.
        /// </summary>
        /// <param name="dto">Nombre de usuario, email y contraseña (mínimo 6 caracteres).</param>
        /// <returns>200 si se registra; 400 si el email ya existe.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (!result)
                return BadRequest("Email already exists");

            return Ok(new { message = "User registered successfully" });
        }

        /// <summary>
        /// Valida las credenciales y devuelve un token JWT si son correctas.
        /// </summary>
        /// <param name="dto">Email y contraseña.</param>
        /// <returns>200 con el token; 401 si las credenciales no son válidas.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);

            if (token == null)
                return Unauthorized("Invalid credentials");

            return Ok(new { token });
        }
    }
}

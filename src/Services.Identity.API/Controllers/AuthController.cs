using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Services.Identity.API.Data;

namespace Services.Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validación directa en duro solicitada ("admin" / "admin")
                if (request != null && request.Username == "admin" && request.Password == "admin")
                {
                    var token = GenerateJwtToken(request.Username);

                    return Ok(new
                    {
                        success = true,
                        token = token,
                        user = request.Username,
                        message = "Acceso concedido"
                    });
                }

                return Unauthorized(new { success = false, message = "Usuario o contraseña incorrectos." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error interno en el servidor: {ex.Message}" });
            }
        }

        private string GenerateJwtToken(string username)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var secretKey = jwtSettings["Key"] ?? "ClaveSecretaTemporalParaMakitaPE2026BaseSegura";
            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = jwtSettings["Issuer"] ?? "MakitaPE_Backend",
                Audience = jwtSettings["Audience"] ?? "MakitaPE_Frontend",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // DTO para recibir las credenciales desde React
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
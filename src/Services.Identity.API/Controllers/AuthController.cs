using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Identity.API.Data;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace Services.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    // Aquí le entregamos las herramientas al Controlador (Base de datos y Configuración)
    public AuthController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Va a la base de datos a buscar si el usuario existe y está activo
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        // 2. Si no existe o la contraseña es incorrecta, le niega el acceso
        if (user == null || user.PasswordHash != request.Password)
            return Unauthorized("Credenciales incorrectas");

        // 3. Si todo está bien, le genera su "Pasaporte" (Token)
        var token = GenerateJwtToken(user);

        // 4. Se lo devuelve al usuario (Frontend)
        return Ok(new { token = token, username = user.Username });
    }

    // Esta es la "Máquina de hacer Pasaportes"
    private string GenerateJwtToken(Models.User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Este es un "molde pequeño" solo para recibir los datos desde internet
public record LoginRequest(string Username, string Password);
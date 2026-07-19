using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly TransporteContext _context;
        private readonly IConfiguration _config;

        public AuthController(TransporteContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Nombre == request.Nombre && u.Clave == request.Clave);

            if (usuario == null)
                return Unauthorized(new { message = "Credenciales inválidas" });

            // Validar estado
            if (!usuario.Estado??false)
                return Unauthorized(new { message = "Usuario inactivo, contacte al administrador" });
            // Generar JWT
            var token = GenerateJwtToken(usuario);

            return Ok(new { token, usuario });
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Nombre),
                new Claim("id", usuario.Id.ToString()),
                new Claim("estado", usuario.Estado.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public bool Estado { get; set; } = false;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using System.Collections.Concurrent; // para ConcurrentDictionary
using System.Net;
using System.Net.Mail;               // para enviar correos SMTP
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly TransporteContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IEncryptionService _encryptionService;

        public AuthController(TransporteContext context, IConfiguration config, IEmailService emailService, IEncryptionService encryptionService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _encryptionService = encryptionService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                    .ThenInclude(r => r.Menus)
                .Include(u => u.PuntoVenta) // 👈 incluir punto de venta
                .FirstOrDefaultAsync(u => u.Usuario1 == request.Nombre);

            if (usuario == null || _encryptionService.Decrypt(usuario.Clave ?? string.Empty) != request.Clave)
                return Unauthorized(new { message = "Credenciales inválidas" });

            if (!(usuario.Estado ?? false))
                return Unauthorized(new { message = "Usuario inactivo, contacte al administrador" });

            if (!(usuario.Acceso ?? false))
                return Unauthorized(new { message = "Usuario sin acceso, contacte al administrador" });

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(usuario);

            var usuarioResponse = new
            {
                usuario.Id,
                usuario.Usuario1,
                usuario.Estado,
                usuario.UltimoAcceso,
                Rol = new
                {
                    usuario.Rol?.Id,
                    usuario.Rol?.Nombre,
                    usuario.Rol?.Estado,
                    Menus = usuario.Rol?.Menus.Select(m => new
                    {
                        m.Id,
                        m.Nombre,
                        m.Icono,
                        m.RutaAccion,
                        m.Tipo,
                        m.Orden,
                        m.PadreId
                    })
                },
                PuntoVenta = usuario.PuntoVenta == null ? null : new
                {
                    usuario.PuntoVenta.Id,
                    usuario.PuntoVenta.Nombre,
                    usuario.PuntoVenta.Direccion,
                    usuario.PuntoVenta.Telefono
                }
            };

            return Ok(new { token, usuario = usuarioResponse });
        }

        [HttpPost("login/send-code")]
        public async Task<IActionResult> SendLoginCode([FromBody] JsonElement raw)
        {
            try
            {
                string usuario = raw.GetProperty("usuario").GetString() ?? string.Empty;
                string correo = raw.GetProperty("correo").GetString() ?? string.Empty;

                var usuarioDb = await _context.Usuarios.FirstOrDefaultAsync(u => u.Usuario1 == usuario);
                if (usuarioDb == null)
                    return StatusCode(500, new { message = "Usuario no encontrado" });

                // Generar código de 6 dígitos
                var rng = new Random();
                string code = rng.Next(100000, 999999).ToString();

                // Guardar en memoria con expiración (ejemplo simple con ConcurrentDictionary)
                _codes[usuario] = new CodeEntry { Code = code, Expiration = DateTime.UtcNow.AddMinutes(3) };

                // Enviar correo
                await _emailService.SendEmailAsync(correo, "Código de acceso", $"Tu código es: {code}");

                return Ok(new { mensaje = "Código enviado al correo", usuario });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error enviando código", detalle = ex.Message });
            }
        }

        // Diccionario en memoria (puedes usar Redis o DB en producción)
        private static readonly ConcurrentDictionary<string, CodeEntry> _codes = new();

        public class CodeEntry
        {
            public string Code { get; set; }
            public DateTime Expiration { get; set; }
        }
        [HttpPost("login/verify-code")]
public async Task<IActionResult> VerifyLoginCode([FromBody] JsonElement raw)
{
    try
    {
        string usuario = raw.GetProperty("usuario").GetString() ?? string.Empty;
        string codigo = raw.GetProperty("codigo").GetString() ?? string.Empty;
        string nuevaClave = raw.GetProperty("nuevaClave").GetString() ?? string.Empty;

        if (!_codes.TryGetValue(usuario, out var entry))
            return BadRequest(new { mensaje = "No se encontró código para este usuario" });

        if (entry.Expiration <= DateTime.UtcNow)
        {
            _codes.TryRemove(usuario, out _);
            return BadRequest(new { mensaje = "El código ha expirado" });
        }

        if (entry.Code != codigo)
            return BadRequest(new { mensaje = "Código inválido" });

        var usuarioDb = await _context.Usuarios.FirstOrDefaultAsync(u => u.Usuario1 == usuario);
        if (usuarioDb == null)
            return BadRequest(new { mensaje = "Usuario no encontrado" });

        usuarioDb.Clave = _encryptionService.Encrypt(nuevaClave);
        await _context.SaveChangesAsync();

        // Código válido y usado, limpiar para que no se reutilice
        _codes.TryRemove(usuario, out _);

        return Ok(new { mensaje = "Contraseña actualizada correctamente" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "Error validando código", detalle = ex.Message });
    }
}
        private string GenerateJwtToken(Usuario usuario)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Usuario1 ?? string.Empty),
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using System.Text.Json;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly TransporteContext _context;

        public UsuariosController(TransporteContext context)
        {
            _context = context;
        }

       // GET: api/usuarios
[HttpGet]
public async Task<ActionResult<IEnumerable<object>>> GetUsuarios()
{
    var usuarios = await _context.Usuarios
        .Where(u => u.Estado == true) // solo activos
        .Include(u => u.Rol)
            .ThenInclude(r => r.Menus)
        .Select(u => new
        {
            u.Id,
            Usuario1 = u.Usuario1 ?? string.Empty,
            Estado = u.Estado ?? false,
            UltimoAcceso = u.UltimoAcceso ?? string.Empty,
            Acceso = u.Acceso ?? false,
            Rol = u.Rol == null ? null : new
            {
                Id = u.Rol.Id,
                Nombre = u.Rol.Nombre ?? string.Empty,
                Estado = u.Rol.Estado ?? false,
                Menus = u.Rol.Menus.Select(m => new
                {
                    m.Id,
                    Nombre = m.Nombre ?? string.Empty,
                    Icono = m.Icono ?? string.Empty,
                    RutaAccion = m.RutaAccion ?? string.Empty,
                    Tipo = m.Tipo ?? string.Empty,
                    Orden = m.Orden ?? 0,
                    PadreId = m.PadreId
                })
            }
        })
        .ToListAsync();

    return Ok(usuarios);
}

        // GET: api/usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                    .ThenInclude(r => r.Menus)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return NotFound();

            return usuario;
        }

        // POST: api/usuarios
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            Console.WriteLine($"-----------------------Usuario llegando- CREAR----------------------------------: {JsonSerializer.Serialize(usuario)}----------");
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        // PUT: api/usuarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            Console.WriteLine($"-----------------------Usuario llegando---MODIFICAR--------------------------------: {JsonSerializer.Serialize(usuario)}----------id-{id}");
            if (id != usuario.Id)
                return BadRequest();

            _context.Entry(usuario).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Usuarios.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            // En lugar de eliminar, marcamos Estado como false
            usuario.Estado = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // PUT: api/usuarios/acceso/5
        [HttpPut("acceso/{id}")]
        public async Task<IActionResult> ToggleAcceso(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            // Alternar el valor de acceso (true ↔ false)
            usuario.Acceso = !(usuario.Acceso ?? false);

            await _context.SaveChangesAsync();

            return Ok(usuario); // puedes devolver el usuario actualizado
        }

    }
}

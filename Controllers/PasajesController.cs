using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasajesController : ControllerBase
    {
        private readonly TransporteContext _context;

        public PasajesController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/pasajes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PasajeListDto>>> GetPasajes()
        {
            var list = await _context.Pasajes
                .Include(p => p.Cliente)
                .Include(p => p.Asiento)
                .Include(p => p.Horario).ThenInclude(h => h.Vehiculo).ThenInclude(v => v.Conductor)
                .Include(p => p.Usuario)
                .Include(p => p.UsuarioAnula)
                .ToListAsync();

            var result = list.Select(p => new PasajeListDto
            {
                Id = p.Id,
                FechaHora = p.FechaHora,
                Monto = p.Monto,
                Movil = p.Movil,
                Estado = p.Estado,
                Destino = p.Destino,
                Asiento = p.Asiento == null ? null : new AsientoDto
                {
                    Id = p.Asiento.Id,
                    Fila = p.Asiento.Fila,
                    Columna = p.Asiento.Columna,
                    Estado = p.Asiento.Estado,
                    Numero = p.Asiento.Numero
                },
                Cliente = p.Cliente == null ? null : new ClienteDto
                {
                    Id = p.Cliente.Id,
                    NombreCompleto = p.Cliente.NombreCompleto,
                    Ci = p.Cliente.Ci,
                    Telefono = p.Cliente.Telefono,
                    Estado = p.Cliente.Estado
                },
                Usuario = p.Usuario == null ? null : new UsuarioDto
                {
                    Id = p.Usuario.Id,
                    Usuario = p.Usuario.Usuario1,
                    PuntoVentaId = p.Usuario.PuntoVentaId,
                    RolId = p.Usuario.RolId
                },
                UsuarioAnula = p.UsuarioAnula == null ? null : new UsuarioDto
                {
                    Id = p.UsuarioAnula.Id,
                    Usuario = p.UsuarioAnula.Usuario1,
                    PuntoVentaId = p.UsuarioAnula.PuntoVentaId,
                    RolId = p.UsuarioAnula.RolId
                }
            }).ToList();

            return Ok(result);
        }

        // GET: api/pasajes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PasajeListDto>> GetPasaje(int id)
        {
            var p = await _context.Pasajes
                .Include(p => p.Cliente)
                .Include(p => p.Asiento)
                .Include(p => p.Horario).ThenInclude(h => h.Vehiculo).ThenInclude(v => v.Conductor)
                .Include(p => p.Usuario)
                .Include(p => p.UsuarioAnula)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return NotFound();

            var dto = new PasajeListDto
            {
                Id = p.Id,
                FechaHora = p.FechaHora,
                Monto = p.Monto,
                Movil = p.Movil,
                Estado = p.Estado,
                Destino = p.Destino,
                Asiento = p.Asiento == null ? null : new AsientoDto
                {
                    Id = p.Asiento.Id,
                    Fila = p.Asiento.Fila,
                    Columna = p.Asiento.Columna,
                    Estado = p.Asiento.Estado,
                    Numero = p.Asiento.Numero
                },
                Cliente = p.Cliente == null ? null : new ClienteDto
                {
                    Id = p.Cliente.Id,
                    NombreCompleto = p.Cliente.NombreCompleto,
                    Ci = p.Cliente.Ci,
                    Telefono = p.Cliente.Telefono,
                    Estado = p.Cliente.Estado
                },
                Usuario = p.Usuario == null ? null : new UsuarioDto
                {
                    Id = p.Usuario.Id,
                    Usuario = p.Usuario.Usuario1,
                    PuntoVentaId = p.Usuario.PuntoVentaId,
                    RolId = p.Usuario.RolId
                },
                UsuarioAnula = p.UsuarioAnula == null ? null : new UsuarioDto
                {
                    Id = p.UsuarioAnula.Id,
                    Usuario = p.UsuarioAnula.Usuario1,
                    PuntoVentaId = p.UsuarioAnula.PuntoVentaId,
                    RolId = p.UsuarioAnula.RolId
                }
            };

            return Ok(dto);
        }

        // POST: api/pasajes
        [HttpPost]
        public async Task<ActionResult<Pasaje>> PostPasaje(Pasaje pasaje)
        {
            _context.Pasajes.Add(pasaje);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPasaje), new { id = pasaje.Id }, pasaje);
        }

        // PUT: api/pasajes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPasaje(int id, Pasaje pasaje)
        {
            if (id != pasaje.Id) return BadRequest();

            _context.Entry(pasaje).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Pasajes.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return Ok(pasaje);
        }

        // DELETE: api/pasajes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePasaje(int id)
        {
            var pasaje = await _context.Pasajes.FindAsync(id);
            if (pasaje == null) return NotFound();

            pasaje.Estado = false; // marcar como inactivo en lugar de eliminar
            pasaje.UsuarioAnulaId = /* aquí asignas el usuario que anula */ null;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

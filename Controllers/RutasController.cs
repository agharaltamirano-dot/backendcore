using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RutasController : ControllerBase
    {
        private readonly TransporteContext _context;

        public RutasController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/rutas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RutaDto>>> GetRutas()
        {
            var rutas = await _context.Ruta
                // .Where(c => c.Estado == true)
                .Include(r => r.Destinos).ThenInclude(d => d.PuntoVenta)
                .ToListAsync();

            var result = rutas.Select(r => new RutaDto
            {
                Id = r.Id,
                Dias = r.Dias,
                Tarifa = r.Tarifa,
                Estado = r.Estado,
                Destinos = r.Destinos.Select(d => new DestinoDto { Id = d.Id, EsOrigen = d.EsOrigen, Orden = d.Orden, PuntoVenta = d.PuntoVenta == null ? null : new PuntoVentaDto { Id = d.PuntoVenta.Id, Nombre = d.PuntoVenta.Nombre, Direccion = d.PuntoVenta.Direccion, Telefono = d.PuntoVenta.Telefono } }).ToList()
            }).Reverse().ToList();

            return Ok(result);
        }

        // GET: api/rutas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RutaDto>> GetRuta(int id)
        {
            var ruta = await _context.Ruta
                .Include(r => r.Horarios)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ruta == null)
            {
                return NotFound();
            }

            return Ok(new RutaDto
            {
                Id = ruta.Id,
                Dias = ruta.Dias,
                Tarifa = ruta.Tarifa,
                Estado = ruta.Estado,
                Destinos = ruta.Destinos.Select(d => new DestinoDto { Id = d.Id, EsOrigen = d.EsOrigen, Orden = d.Orden, PuntoVenta = d.PuntoVenta == null ? null : new PuntoVentaDto { Id = d.PuntoVenta.Id, Nombre = d.PuntoVenta.Nombre, Direccion = d.PuntoVenta.Direccion, Telefono = d.PuntoVenta.Telefono } }).ToList()
            });
        }

        // POST: api/rutas
        // POST: api/rutas
[HttpPost]
public async Task<ActionResult<Rutum>> PostRuta(RutaWriteDto dto)
{
    var ruta = new Rutum
    {
        Dias = dto.Dias,
        Tarifa = dto.Tarifa,
        Estado = dto.Estado,
        Destinos = dto.Destinos?.Select(d => new Destino
        {
            EsOrigen = d.EsOrigen,
            Orden = d.Orden,
            PuntoVentaId = d.PuntoVentaId,
            Tarifa = d.Tarifa
        }).ToList() ?? new List<Destino>()
    };

    _context.Ruta.Add(ruta);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetRuta), new { id = ruta.Id }, ruta);
}


[HttpPut("{id}")]
public async Task<IActionResult> PutRuta(int id, RutaWriteDto dto)
{
    var ruta = await _context.Ruta
        .Include(r => r.Destinos)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (ruta == null) return NotFound();

    ruta.Dias = dto.Dias;
    ruta.Tarifa = dto.Tarifa;
    ruta.Estado = dto.Estado;

    var destinosExistentes = ruta.Destinos.ToList();

    foreach (var destinoDto in dto.Destinos ?? new List<DestinoWriteDto>())
    {
        var destinoExistente = destinosExistentes.FirstOrDefault(d => d.Id == destinoDto.Id);

        if (destinoExistente != null)
        {
            destinoExistente.EsOrigen = destinoDto.EsOrigen;
            destinoExistente.Orden = destinoDto.Orden;
            destinoExistente.PuntoVentaId = destinoDto.PuntoVentaId;
            destinoExistente.Tarifa = destinoDto.Tarifa;
        }
        else
        {
            ruta.Destinos.Add(new Destino
            {
                EsOrigen = destinoDto.EsOrigen,
                Orden = destinoDto.Orden,
                PuntoVentaId = destinoDto.PuntoVentaId,
                Tarifa = destinoDto.Tarifa
            });
        }
    }

    var idsDto = dto.Destinos?.Select(d => d.Id).ToList() ?? new List<int>();
    foreach (var destino in destinosExistentes)
    {
        if (!idsDto.Contains(destino.Id))
        {
            _context.Destinos.Remove(destino);
        }
    }

    await _context.SaveChangesAsync();
    return Ok(ruta);
}



        // DELETE: api/rutas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRuta(int id)
        {
            var ruta = await _context.Ruta.FindAsync(id);
            if (ruta == null)
            {
                return NotFound();
            }

            // _context.Ruta.Remove(ruta);
            ruta.Estado = !ruta.Estado; // Cambiar el estado a "Inactivo" en lugar de eliminar
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

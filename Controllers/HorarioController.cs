using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HorariosController : ControllerBase
    {
        private readonly TransporteContext _context;

        public HorariosController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/horarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HorarioListDto>>> GetHorarios()
        {
            var horarios = await _context.Horarios
                .Where(h => h.Estado == true)
                .Include(h => h.Ruta).ThenInclude(r => r.Destinos).ThenInclude(d => d.PuntoVenta)
                .Include(h => h.Vehiculo).ThenInclude(v => v.Conductor)
                .Include(h => h.Vehiculo).ThenInclude(v => v.Distribucion).ThenInclude(d => d.Asientos)
                .ToListAsync();

            var result = horarios.Select(h => new HorarioListDto
            {
                Id = h.Id,
                Fecha = h.Fecha,
                Hora = h.Hora,
                Estado = h.Estado,
                Ruta = h.Ruta == null ? null : new RutaDto
                {
                    Id = h.Ruta.Id,
                    Dias = h.Ruta.Dias,
                    Tarifa = h.Ruta.Tarifa,
                    Estado = h.Ruta.Estado,
                    Destinos = h.Ruta.Destinos.Select(d => new DestinoDto
                    {
                        Id = d.Id,
                        EsOrigen = d.EsOrigen,
                        Orden = d.Orden,
                        PuntoVenta = d.PuntoVenta == null ? null : new PuntoVentaDto
                        {
                            Id = d.PuntoVenta.Id,
                            Nombre = d.PuntoVenta.Nombre,
                            Direccion = d.PuntoVenta.Direccion,
                            Telefono = d.PuntoVenta.Telefono
                        }
                    }).ToList()
                },
                Vehiculo = h.Vehiculo == null ? null : new VehiculoListLiteDto
                {
                    Id = h.Vehiculo.Id,
                    Placa = h.Vehiculo.Placa,
                    Movil = h.Vehiculo.Movil,
                    Conductor = h.Vehiculo.Conductor == null ? null : new ConductorDto
                    {
                        Id = h.Vehiculo.Conductor.Id,
                        Nombres = h.Vehiculo.Conductor.Nombres,
                        Apellidos = h.Vehiculo.Conductor.Apellidos,
                        Telefono = h.Vehiculo.Conductor.Telefono
                    }
                }
            }).Reverse().ToList();

            return Ok(result);
        }

        // GET: api/horarios/5
[HttpGet("{id}")]
public async Task<ActionResult<HorarioListDto>> GetHorario(int id)
{
    var h = await _context.Horarios
        .Include(h => h.Ruta).ThenInclude(r => r.Destinos).ThenInclude(d => d.PuntoVenta)
        .Include(h => h.Vehiculo).ThenInclude(v => v.Conductor)
        .Include(h => h.Vehiculo).ThenInclude(v => v.Distribucion).ThenInclude(d => d.Asientos)
        .FirstOrDefaultAsync(h => h.Id == id);

    if (h == null) return NotFound();

    var dto = new HorarioListDto
    {
        Id = h.Id,
        Fecha = h.Fecha,
        Hora = h.Hora,
        Estado = h.Estado,
        Ruta = h.Ruta == null ? null : new RutaDto
        {
            Id = h.Ruta.Id,
            Dias = h.Ruta.Dias,
            Tarifa = h.Ruta.Tarifa,
            Estado = h.Ruta.Estado,
            Destinos = h.Ruta.Destinos.Select(d => new DestinoDto
            {
                Id = d.Id,
                EsOrigen = d.EsOrigen,
                Orden = d.Orden,
                PuntoVenta = d.PuntoVenta == null ? null : new PuntoVentaDto
                {
                    Id = d.PuntoVenta.Id,
                    Nombre = d.PuntoVenta.Nombre,
                    Direccion = d.PuntoVenta.Direccion,
                    Telefono = d.PuntoVenta.Telefono
                }
            }).ToList()
        },
        Vehiculo = h.Vehiculo == null ? null : new VehiculoListLiteDto
        {
            Id = h.Vehiculo.Id,
            Placa = h.Vehiculo.Placa,
            Movil = h.Vehiculo.Movil,
            Conductor = h.Vehiculo.Conductor == null ? null : new ConductorDto
            {
                Id = h.Vehiculo.Conductor.Id,
                Nombres = h.Vehiculo.Conductor.Nombres,
                Apellidos = h.Vehiculo.Conductor.Apellidos,
                Telefono = h.Vehiculo.Conductor.Telefono
            },
            Distribucion = h.Vehiculo.Distribucion == null ? null : new DistribucionDto
            {
                Id = h.Vehiculo.Distribucion.Id,
                Estado = h.Vehiculo.Distribucion.Estado,
                Nombre = h.Vehiculo.Distribucion.Nombre,
                Asientos = h.Vehiculo.Distribucion.Asientos.Select(a => new AsientoDto
                {
                    Id = a.Id,
                    Fila = a.Fila,
                    Columna = a.Columna,
                    Estado = a.Estado,
                    Numero = a.Numero
                }).ToList()
            }
        },
    };

    return Ok(dto);
}
        // POST: api/horarios
        [HttpPost]
        public async Task<ActionResult<Horario>> PostHorario(Horario horario)
        {
            _context.Horarios.Add(horario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHorario), new { id = horario.Id }, horario);
        }

        // PUT: api/horarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHorario(int id, Horario horario)
        {
            if (id != horario.Id) return BadRequest();

            _context.Entry(horario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Horarios.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return Ok(horario);
        }

        // DELETE: api/horarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHorario(int id)
        {
            var horario = await _context.Horarios.FindAsync(id);
            if (horario == null) return NotFound();

            horario.Estado = false; // marcar como inactivo
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

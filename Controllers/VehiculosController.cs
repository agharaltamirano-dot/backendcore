using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculosController : ControllerBase
    {
        private readonly TransporteContext _context;

        public VehiculosController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/vehiculos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehiculoListDto>>> GetVehiculos()
        {
            var list = await _context.Vehiculos
                .Where(c => c.Estado == true)
                .Include(v => v.Conductor)
                .Include(v => v.Propietario)
                .Include(v => v.Distribucion).ThenInclude(d => d.Asientos)
                .ToListAsync();

            var result = list.Select(v => new VehiculoListDto
            {
                Id = v.Id,
                Movil = v.Movil,
                Placa = v.Placa,
                Marca = v.Marca,
                Modelo = v.Modelo,
                Estado = v.Estado,
                Color = v.Color,
                Tipo = v.Tipo,
                Soat = v.Soat,
                Aseguradora = v.Aseguradora,
                Conductor = v.Conductor == null ? null : new ConductorDto { Id = v.Conductor.Id, Nombres = v.Conductor.Nombres, Apellidos = v.Conductor.Apellidos, Telefono = v.Conductor.Telefono },
                Propietario = v.Propietario == null ? null : new ConductorDto { Id = v.Propietario.Id, Nombres = v.Propietario.Nombres, Apellidos = v.Propietario.Apellidos, Telefono = v.Propietario.Telefono },
                Distribucion = v.Distribucion == null ? null : new DistribucionDto {
                    Id = v.Distribucion.Id,
                    Estado = v.Distribucion.Estado,
                    Nombre = v.Distribucion.Nombre,
                    Asientos = v.Distribucion.Asientos.Select(a => new AsientoDto {
                        Id = a.Id,
                        Fila = a.Fila,
                        Columna = a.Columna,
                        Estado = a.Estado,
                    }).ToList()
                }
            }).Reverse().ToList();

            return Ok(result);
        }

        // GET: api/vehiculos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehiculoListDto>> GetVehiculo(int id)
        {
            var v = await _context.Vehiculos
                .Include(v => v.Conductor)
                .Include(v => v.Propietario)
                .Include(v => v.Distribucion).ThenInclude(d => d.Asientos)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (v == null) return NotFound();

            var dto = new VehiculoListDto
            {
                Id = v.Id,
                Movil = v.Movil,
                Placa = v.Placa,
                Marca = v.Marca,
                Modelo = v.Modelo,
                Estado = v.Estado,
                Conductor = v.Conductor == null ? null : new ConductorDto { Id = v.Conductor.Id, Nombres = v.Conductor.Nombres, Apellidos = v.Conductor.Apellidos, Telefono = v.Conductor.Telefono },
                Propietario = v.Propietario == null ? null : new ConductorDto { Id = v.Propietario.Id, Nombres = v.Propietario.Nombres, Apellidos = v.Propietario.Apellidos, Telefono = v.Propietario.Telefono },
                Distribucion = v.Distribucion == null ? null : new DistribucionDto { Id = v.Distribucion.Id, Estado = v.Distribucion.Estado, Asientos = v.Distribucion.Asientos.Select(a => new AsientoDto { Id = a.Id, Fila = a.Fila, Columna = a.Columna, Estado = a.Estado }).ToList() }
            };

            return Ok(dto);
        }

        // POST: api/vehiculos
        [HttpPost]
        public async Task<ActionResult<Vehiculo>> PostVehiculo(Vehiculo vehiculo)
        {
            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehiculo), new { id = vehiculo.Id }, vehiculo);
        }

        // PUT: api/vehiculos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehiculo(int id, Vehiculo vehiculo)
        {
            if (id != vehiculo.Id)
            {
                return BadRequest();
            }

            _context.Entry(vehiculo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Vehiculos.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/vehiculos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehiculo(int id)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo == null)
            {
                return NotFound();
            }

            // _context.Vehiculos.Remove(vehiculo);
            vehiculo.Estado = false; // Cambiar el estado a "Inactivo" en lugar de eliminar
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

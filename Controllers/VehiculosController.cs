using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;
using Microsoft.AspNetCore.Hosting;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculosController : ControllerBase
    {
        private readonly TransporteContext _context;

        private readonly IWebHostEnvironment _env;

        public VehiculosController(TransporteContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/vehiculos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehiculoListDto>>> GetVehiculos()
        {
            var list = await _context.Vehiculos
                // .Where(c => c.Estado == true)
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
                Foto = v.Foto,
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
        public async Task<ActionResult<Vehiculo>> PostVehiculo([FromForm] Vehiculo vehiculo, IFormFile? foto)
        {
            if (foto != null && foto.Length > 0)
            {
                var folderPath = Path.Combine(_env.ContentRootPath, "assets/vehiculos");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Nombre único con fecha y hora
                var fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(foto.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                vehiculo.Foto = fileName; // Guardamos solo el nombre en BD
            }

            _context.Vehiculos.Add(vehiculo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehiculo), new { id = vehiculo.Id }, vehiculo);
        }

        // PUT: api/vehiculos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehiculo(int id, [FromForm] Vehiculo vehiculo, IFormFile? foto)
        {
            if (id != vehiculo.Id)
                return BadRequest();

            var existing = await _context.Vehiculos.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Actualizar campos básicos
            existing.Movil = vehiculo.Movil;
            existing.Placa = vehiculo.Placa;
            existing.Marca = vehiculo.Marca;
            existing.Modelo = vehiculo.Modelo;
            existing.Color = vehiculo.Color;
            existing.Tipo = vehiculo.Tipo;
            existing.Soat = vehiculo.Soat;
            existing.Aseguradora = vehiculo.Aseguradora;
            existing.ConductorId = vehiculo.ConductorId;
            existing.PropietarioId = vehiculo.PropietarioId;
            existing.Estado = vehiculo.Estado;
            existing.Activo = vehiculo.Activo;
            existing.DistribucionId = vehiculo.DistribucionId;

            if (foto != null && foto.Length > 0)
            {
                var folderPath = Path.Combine(_env.ContentRootPath, "assets/vehiculos");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(foto.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                existing.Foto = fileName;
            }

            _context.Entry(existing).State = EntityState.Modified;
            await _context.SaveChangesAsync();

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

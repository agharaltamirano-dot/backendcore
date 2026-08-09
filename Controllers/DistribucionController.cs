using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistribucionController : ControllerBase
    {
        private readonly TransporteContext _context;

        public DistribucionController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/distribucion
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DistribucionDto>>> GetDistribuciones()
        {
            var list = await _context.DistribucionAsientos
                .Include(d => d.Asientos)
                    .ThenInclude(a => a.Pasajes)
                        .ThenInclude(p => p.Cliente)
                .ToListAsync();

            var result = list.Select(d => new DistribucionDto
            {
                Id = d.Id,
                Estado = d.Estado,
                Nombre = d.Nombre,
                Asientos = d.Asientos.Select(a => new AsientoDto
                {
                    Id = a.Id,
                    Fila = a.Fila,
                    Columna = a.Columna,
                    Estado = a.Estado,
                    Numero = a.Numero,
                    Pasajes = a.Pasajes.Select(p => new PasajeSummaryDto { Id = p.Id, FechaHora = p.FechaHora, Monto = p.Monto, Estado = p.Estado, Cliente = p.Cliente == null ? null : new ClienteDto { Id = p.Cliente.Id, NombreCompleto = p.Cliente.NombreCompleto, Ci = p.Cliente.Ci, Telefono = p.Cliente.Telefono, Estado = p.Cliente.Estado } }).ToList()
                }).ToList()
            }).ToList();
result.Reverse(); // Invertir el orden de la lista
            return Ok(result);
        }

        // GET: api/distribucion/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DistribucionDto>> GetDistribucion(int id)
        {
            var d = await _context.DistribucionAsientos
                .Include(d => d.Asientos).ThenInclude(a => a.Pasajes).ThenInclude(p => p.Cliente)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (d == null) return NotFound();

            var dto = new DistribucionDto
            {
                Id = d.Id,
                Estado = d.Estado,
                Nombre = d.Nombre,
                Asientos = d.Asientos.Select(a => new AsientoDto
                {
                    Id = a.Id,
                    Fila = a.Fila,
                    Columna = a.Columna,
                    Estado = a.Estado,
                    Numero = a.Numero,
                    Pasajes = a.Pasajes.Select(p => new PasajeSummaryDto { Id = p.Id, FechaHora = p.FechaHora, Monto = p.Monto, Estado = p.Estado, Cliente = p.Cliente == null ? null : new ClienteDto { Id = p.Cliente.Id, NombreCompleto = p.Cliente.NombreCompleto, Ci = p.Cliente.Ci, Telefono = p.Cliente.Telefono, Estado = p.Cliente.Estado } }).ToList()
                }).ToList()
            };

            return Ok(dto);
        }

        // POST: api/distribucion
        [HttpPost]
        public async Task<ActionResult<DistribucionAsiento>> PostDistribucion(DistribucionDto dto)
        {
             System.Console.WriteLine($"---------------- ENTRANDO AL METODO ---------------- {dto.ToString()}");
System.Console.WriteLine($"Nombre: {dto.Nombre}, Estado: {dto.Estado}, Asientos: {dto.Asientos?.Count}");
foreach (var a in dto.Asientos ?? new List<AsientoDto>())
{
    System.Console.WriteLine($"Asiento -> Id: {a.Id}, Fila: {a.Fila}, Columna: {a.Columna}, Estado: {a.Estado}, Numero: {a.Numero}");
}
    
            var distribucion = new DistribucionAsiento
            {
                Estado = dto.Estado,
                Nombre = dto.Nombre,
                Asientos = dto.Asientos?.Select(a => new Asiento
                {
                    Fila = a.Fila,
                    Columna = a.Columna,
                    Estado = a.Estado,
                    Numero = a.Numero
                }).ToList() ?? new List<Asiento>()
            };

            _context.DistribucionAsientos.Add(distribucion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDistribucion), new { id = distribucion.Id }, distribucion);
        }

        // PUT: api/distribucion/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDistribucion(int id, DistribucionDto dto)
        {
            var distribucion = await _context.DistribucionAsientos
                .Include(d => d.Asientos)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (distribucion == null) return NotFound();

            distribucion.Estado = dto.Estado;
            distribucion.Nombre = dto.Nombre;

            // Manejo de Asientos
            var asientosExistentes = distribucion.Asientos.ToList();

            // Actualizar o crear los que llegan
            foreach (var asientoDto in dto.Asientos ?? new List<AsientoDto>())
            {
                var asientoExistente = asientosExistentes.FirstOrDefault(a => a.Id == asientoDto.Id);

                if (asientoExistente != null)
                {
                    // Actualizar asiento existente
                    asientoExistente.Fila = asientoDto.Fila;
                    asientoExistente.Columna = asientoDto.Columna;
                    asientoExistente.Estado = asientoDto.Estado;
                    asientoExistente.Numero = asientoDto.Numero;
                }
                else
                {
                    // Crear nuevo asiento
                    distribucion.Asientos.Add(new Asiento
                    {
                        Fila = asientoDto.Fila,
                        Columna = asientoDto.Columna,
                        Estado = asientoDto.Estado,
                        Numero = asientoDto.Numero
                    });
                }
            }

            // Eliminar los que no llegaron en el DTO
            var idsDto = dto.Asientos?.Select(a => a.Id).ToList() ?? new List<int>();
            foreach (var asiento in asientosExistentes)
            {
                if (!idsDto.Contains(asiento.Id))
                {
                    // Verificar si tiene pasajes activos
                    var tienePasajesActivos = await _context.Pasajes
                        .AnyAsync(p => p.AsientoId == asiento.Id && p.Estado == true);

                    if (tienePasajesActivos)
                    {
                        return BadRequest($"No se puede eliminar el asiento {asiento.Id} porque tiene pasajes activos.");
                    }

                    _context.Asientos.Remove(asiento);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(distribucion);
        }

        // DELETE: api/distribucion/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDistribucion(int id)
        {
            var distribucion = await _context.DistribucionAsientos.FindAsync(id);
            if (distribucion == null) return NotFound();

            distribucion.Estado = !distribucion.Estado; // marcar como inactivo
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

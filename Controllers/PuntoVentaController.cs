using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/puntos-venta")]
    public class PuntoVentaController : ControllerBase
    {
        private readonly TransporteContext _context;

        public PuntoVentaController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/puntos-venta?estado=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPuntosVenta([FromQuery] bool? estado)
        {
            var query = _context.PuntoVenta.AsQueryable();

            if (estado.HasValue)
                query = query.Where(p => p.EsPuntoVenta == estado.Value);

            var puntosVenta = await query
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Direccion,
                    p.Telefono,
                    p.EsPuntoVenta,
                    p.VisiblePasajes
                })
                .ToListAsync();

            puntosVenta.Reverse();
            return Ok(puntosVenta);
        }

        // GET: api/puntos-venta/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPuntoVenta(int id)
        {
            var puntoVenta = await _context.PuntoVenta
                .Where(p => p.Id == id)
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Direccion,
                    p.Telefono,
                    p.EsPuntoVenta,
                    p.VisiblePasajes
                })
                .FirstOrDefaultAsync();

            if (puntoVenta == null)
                return NotFound();

            return Ok(puntoVenta);
        }

        // POST: api/puntos-venta
        [HttpPost]
        public async Task<ActionResult<PuntoVentum>> PostPuntoVenta(PuntoVentum puntoVenta)
        {
            _context.PuntoVenta.Add(puntoVenta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPuntoVenta), new { id = puntoVenta.Id }, puntoVenta);
        }

        // PUT: api/puntos-venta/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPuntoVenta(int id, PuntoVentum puntoVenta)
        {
            if (id != puntoVenta.Id)
                return BadRequest();

            _context.Entry(puntoVenta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PuntoVenta.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return Ok(puntoVenta);
        }

        // DELETE: api/puntos-venta/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePuntoVenta(int id)
        {
            var puntoVenta = await _context.PuntoVenta.FindAsync(id);
            if (puntoVenta == null)
                return NotFound();

            // En lugar de eliminar físicamente, lo marcamos como inactivo
            puntoVenta.EsPuntoVenta = !puntoVenta.EsPuntoVenta;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

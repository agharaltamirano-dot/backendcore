using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;

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
        public async Task<ActionResult<IEnumerable<Rutum>>> GetRutas()
        {
            var rutas = await _context.Ruta
            .Where(c => c.Estado == true)
                .Include(r => r.Origen)
                .Include(r => r.Destino)
                .Include(r => r.Horarios)
                .ToListAsync();
                rutas.Reverse(); // Invertir el orden de las rutas
            return rutas;
        }

        // GET: api/rutas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Rutum>> GetRuta(int id)
        {
            var ruta = await _context.Ruta
                .Include(r => r.Origen)
                .Include(r => r.Destino)
                .Include(r => r.Horarios)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ruta == null)
            {
                return NotFound();
            }

            return ruta;
        }

        // POST: api/rutas
        [HttpPost]
        public async Task<ActionResult<Rutum>> PostRuta(Rutum ruta)
        {
            _context.Ruta.Add(ruta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRuta), new { id = ruta.Id }, ruta);
        }

        // PUT: api/rutas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRuta(int id, Rutum ruta)
        {
            if (id != ruta.Id)
            {
                return BadRequest();
            }

            _context.Entry(ruta).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Ruta.Any(e => e.Id == id))
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
            ruta.Estado = false; // Cambiar el estado a "Inactivo" en lugar de eliminar
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

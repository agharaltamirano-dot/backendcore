using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsientosController : ControllerBase
    {
        private readonly TransporteContext _context;

        public AsientosController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/asientos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asiento>>> GetAsientos()
        {
            return await _context.Asientos
                .Include(a => a.Vehiculos)
                .ToListAsync();
        }

        // GET: api/asientos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Asiento>> GetAsiento(int id)
        {
            var asiento = await _context.Asientos
                .Include(a => a.Vehiculos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asiento == null)
            {
                return NotFound();
            }

            return asiento;
        }

        // POST: api/asientos
        [HttpPost]
        public async Task<ActionResult<Asiento>> PostAsiento(Asiento asiento)
        {
            _context.Asientos.Add(asiento);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAsiento), new { id = asiento.Id }, asiento);
        }

        // PUT: api/asientos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsiento(int id, Asiento asiento)
        {
            if (id != asiento.Id)
            {
                return BadRequest();
            }

            _context.Entry(asiento).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Asientos.Any(e => e.Id == id))
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

        // DELETE: api/asientos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsiento(int id)
        {
            var asiento = await _context.Asientos.FindAsync(id);
            if (asiento == null)
            {
                return NotFound();
            }

            // _context.Asientos.Remove(asiento);
            asiento.Estado = false; // Cambiar el estado a "Inactivo" en lugar de eliminar
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

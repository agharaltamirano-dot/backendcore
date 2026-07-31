using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConductoresController : ControllerBase
    {
        private readonly TransporteContext _context;

        public ConductoresController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/conductores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Conductor>>> GetConductores()
        {
            var conductores = await _context.Conductors.Where(c => c.Estado == true).ToListAsync(); // solo activos
            conductores.Reverse(); // Invertir el orden de los conductores
            return conductores;
        }

        // GET: api/conductores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Conductor>> GetConductor(int id)
        {
            var conductor = await _context.Conductors.FindAsync(id);

            if (conductor == null)
            {
                return NotFound();
            }

            return conductor;
        }

        // POST: api/conductores
        [HttpPost]
        public async Task<ActionResult<Conductor>> PostConductor(Conductor conductor)
        {
            _context.Conductors.Add(conductor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConductor), new { id = conductor.Id }, conductor);
        }

        // PUT: api/conductores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutConductor(int id, Conductor conductor)
        {
            if (id != conductor.Id)
            {
                return BadRequest();
            }

            _context.Entry(conductor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Conductors.Any(e => e.Id == id))
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

        // DELETE: api/conductores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConductor(int id)
        {
            var conductor = await _context.Conductors.FindAsync(id);
            if (conductor == null)
            {
                return NotFound();
            }

            // _context.Conductors.Remove(conductor);
            //solo actualizar estado a false
            conductor.Estado = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

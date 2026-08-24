using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using Microsoft.AspNetCore.Hosting;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConductoresController : ControllerBase
    {
        private readonly TransporteContext _context;
        private readonly IWebHostEnvironment _env;

        public ConductoresController(TransporteContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
        public async Task<ActionResult<Conductor>> PostConductor([FromForm] Conductor conductor, IFormFile? foto_licencia)
        {
            if (foto_licencia != null && foto_licencia.Length > 0)
            {
                var folderPath = Path.Combine(_env.ContentRootPath, "assets/licencias");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Nombre único con fecha y hora
                var fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(foto_licencia.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await foto_licencia.CopyToAsync(stream);
                }

                conductor.FotoLicencia = fileName; // Guardamos solo el nombre en BD
            }

            _context.Conductors.Add(conductor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConductor), new { id = conductor.Id }, conductor);
        }

        // PUT: api/conductores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutConductor(int id, [FromForm] Conductor conductor, IFormFile? foto_licencia)
        {
            if (id != conductor.Id)
                return BadRequest();

            var existing = await _context.Conductors.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Actualizar campos básicos
            existing.Nombres = conductor.Nombres;
            existing.Apellidos = conductor.Apellidos;
            existing.Telefono = conductor.Telefono;
            existing.Categoria = conductor.Categoria;
            existing.Estado = conductor.Estado;

            if (foto_licencia != null && foto_licencia.Length > 0)
            {
                var folderPath = Path.Combine(_env.ContentRootPath, "assets/licencias/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(foto_licencia.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await foto_licencia.CopyToAsync(stream);
                }

                existing.FotoLicencia = fileName;
            }

            _context.Entry(existing).State = EntityState.Modified;
            await _context.SaveChangesAsync();

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

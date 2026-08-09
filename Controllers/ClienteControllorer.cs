using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly TransporteContext _context;

        public ClientesController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/clientes?estado=true&ci=123456&nombreCompleto=Juan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteDto>>> GetClientes(
            [FromQuery] bool? estado,
            [FromQuery] string? ci,
            [FromQuery] string? nombreCompleto)
        {
            var query = _context.Clientes.AsQueryable();

            if (estado.HasValue)
                query = query.Where(c => c.Estado == estado);

            if (!string.IsNullOrEmpty(ci))
                query = query.Where(c => c.Ci == ci);

            if (!string.IsNullOrEmpty(nombreCompleto))
                query = query.Where(c => c.NombreCompleto.Contains(nombreCompleto));

            //var list = await query.Select(c => new ClienteDto { Id = c.Id, NombreCompleto = c.NombreCompleto, Ci = c.Ci, Telefono = c.Telefono, Estado = c.Estado }).ToListAsync();
var list = await _context.Clientes
                .Where(c => !estado.HasValue || c.Estado == estado)
                .Where(c => string.IsNullOrEmpty(ci) || c.Ci == ci)
                .Where(c => string.IsNullOrEmpty(nombreCompleto) || c.NombreCompleto.Contains(nombreCompleto))
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    NombreCompleto = c.NombreCompleto,
                    Ci = c.Ci,
                    Telefono = c.Telefono,
                    Estado = c.Estado
                })
                .ToListAsync();
            return Ok(list);
        }

        // GET: api/clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Pasajes)
                .Include(c => c.EncomiendumClienteConsignatarios)
                .Include(c => c.EncomiendumClienteRemitentes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return cliente;
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        // PUT: api/clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            if (id != cliente.Id)
            {
                return BadRequest();
            }

            _context.Entry(cliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clientes.Any(e => e.Id == id))
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

        // DELETE: api/clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

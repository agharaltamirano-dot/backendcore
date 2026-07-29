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

        // GET: api/puntos-venta
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PuntoVentum>>> GetPuntosVenta()
        {
            // Solo puntos de venta, sin usuarios
            var puntosVenta = await _context.PuntoVenta
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Direccion,
                    p.Telefono
                })
                .ToListAsync();

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
                    p.Telefono
                })
                .FirstOrDefaultAsync();

            if (puntoVenta == null)
                return NotFound();

            return Ok(puntoVenta);
        }
    }
}

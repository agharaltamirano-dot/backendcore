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
    public class EncomiendaController : ControllerBase
    {
        private readonly TransporteContext _context;

        public EncomiendaController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/encomienda
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EncomiendaListDto>>> GetEncomiendas()
        {
            var list = await _context.Encomienda
                .Include(e => e.ClienteRemitente)
                .Include(e => e.ClienteConsignatario)
                .Include(e => e.Usuario)
                .ToListAsync();

            var result = list.Select(e => new EncomiendaListDto
            {
                Id = e.Id,
                Contenido = e.Contenido,
                FechaRecepcion = e.FechaRecepcion,
                FechaEntrega = e.FechaEntrega,
                Monto = e.Monto,
                Numero = e.Numero,
                Estado = e.Estado,
                Pagado = e.Pagado,
                Destino = e.Destino,
                ClienteRemitente = e.ClienteRemitente == null ? null : new ClienteDto { Id = e.ClienteRemitente.Id, NombreCompleto = e.ClienteRemitente.NombreCompleto, Ci = e.ClienteRemitente.Ci, Telefono = e.ClienteRemitente.Telefono, Estado = e.ClienteRemitente.Estado },
                ClienteConsignatario = e.ClienteConsignatario == null ? null : new ClienteDto { Id = e.ClienteConsignatario.Id, NombreCompleto = e.ClienteConsignatario.NombreCompleto, Ci = e.ClienteConsignatario.Ci, Telefono = e.ClienteConsignatario.Telefono, Estado = e.ClienteConsignatario.Estado },
                Usuario = e.Usuario == null ? null : new UsuarioDto { Id = e.Usuario.Id, Usuario = e.Usuario.Usuario1, PuntoVentaId = e.Usuario.PuntoVentaId, RolId = e.Usuario.RolId }
            }).Reverse().ToList();

            return Ok(result);
        }
        // POST: api/encomienda
[HttpPost]
public async Task<ActionResult> PostEncomienda([FromBody] Encomiendum encomienda)
{
    // Si viene objeto ClienteRemitente en vez de Id
    if (encomienda.ClienteRemitenteId == null && encomienda.ClienteRemitente != null)
    {
        _context.Clientes.Add(encomienda.ClienteRemitente);
        await _context.SaveChangesAsync();
        encomienda.ClienteRemitenteId = encomienda.ClienteRemitente.Id;
    }

    // Si viene objeto ClienteConsignatario en vez de Id
    if (encomienda.ClienteConsignatarioId == null && encomienda.ClienteConsignatario != null)
    {
        _context.Clientes.Add(encomienda.ClienteConsignatario);
        await _context.SaveChangesAsync();
        encomienda.ClienteConsignatarioId = encomienda.ClienteConsignatario.Id;
    }

    // Guardar la encomienda para que EF genere el Id
    _context.Encomienda.Add(encomienda);
    await _context.SaveChangesAsync();

    // Ahora que ya tiene Id, generamos el Numero
    encomienda.Numero = $"E-{encomienda.Id}";

    // Actualizamos el registro con el nuevo Numero
    _context.Entry(encomienda).Property(e => e.Numero).IsModified = true;
    await _context.SaveChangesAsync();

    // Mostrar en consola lo recibido
    Console.WriteLine("📦 Encomienda creada:");
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(encomienda));

    return CreatedAtAction(nameof(GetEncomiendas), new { id = encomienda.Id }, encomienda);
}


    }
    
}
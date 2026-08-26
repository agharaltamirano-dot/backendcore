using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;
using System.Text.Json;

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
// PUT: api/encomienda/5
[HttpPut("{id}")]
[Consumes("application/json")]
public async Task<IActionResult> PutEncomienda(int id, [FromBody] Encomiendum dto)
{
    try
    {
        var encomienda = await _context.Encomienda
            .Include(e => e.ClienteRemitente)
            .Include(e => e.ClienteConsignatario)
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (encomienda == null)
            return NotFound(new { mensaje = "Encomienda no encontrada." });

        // Manejo de ClienteRemitente
        if (dto.ClienteRemitenteId == null && dto.ClienteRemitente != null)
        {
            var newCliente = new Cliente
            {
                NombreCompleto = dto.ClienteRemitente.NombreCompleto,
                Ci = dto.ClienteRemitente.Ci,
                Telefono = dto.ClienteRemitente.Telefono,
                Estado = dto.ClienteRemitente.Estado
            };
            _context.Clientes.Add(newCliente);
            await _context.SaveChangesAsync();
            encomienda.ClienteRemitenteId = newCliente.Id;
        }
        else if (dto.ClienteRemitenteId.HasValue)
        {
            encomienda.ClienteRemitenteId = dto.ClienteRemitenteId;
        }

        // Manejo de ClienteConsignatario
        if (dto.ClienteConsignatarioId == null && dto.ClienteConsignatario != null)
        {
            var newCliente = new Cliente
            {
                NombreCompleto = dto.ClienteConsignatario.NombreCompleto,
                Ci = dto.ClienteConsignatario.Ci,
                Telefono = dto.ClienteConsignatario.Telefono,
                Estado = dto.ClienteConsignatario.Estado
            };
            _context.Clientes.Add(newCliente);
            await _context.SaveChangesAsync();
            encomienda.ClienteConsignatarioId = newCliente.Id;
        }
        else if (dto.ClienteConsignatarioId.HasValue)
        {
            encomienda.ClienteConsignatarioId = dto.ClienteConsignatarioId;
        }

        // Actualizar campos
        encomienda.Contenido = dto.Contenido;
        encomienda.FechaRecepcion = dto.FechaRecepcion;
        encomienda.FechaEntrega = dto.FechaEntrega;
        encomienda.Monto = dto.Monto;
        encomienda.Estado = dto.Estado;
        encomienda.Pagado = dto.Pagado;
        encomienda.Destino = dto.Destino;
        encomienda.UsuarioId = dto.UsuarioId;

        await _context.SaveChangesAsync();

        // Proyección a DTO para evitar ciclos
        var result = new EncomiendaListDto
        {
            Id = encomienda.Id,
            Contenido = encomienda.Contenido,
            FechaRecepcion = encomienda.FechaRecepcion,
            FechaEntrega = encomienda.FechaEntrega,
            Monto = encomienda.Monto,
            Numero = encomienda.Numero,
            Estado = encomienda.Estado,
            Pagado = encomienda.Pagado,
            Destino = encomienda.Destino,
            ClienteRemitente = encomienda.ClienteRemitente == null ? null : new ClienteDto
            {
                Id = encomienda.ClienteRemitente.Id,
                NombreCompleto = encomienda.ClienteRemitente.NombreCompleto,
                Ci = encomienda.ClienteRemitente.Ci,
                Telefono = encomienda.ClienteRemitente.Telefono,
                Estado = encomienda.ClienteRemitente.Estado
            },
            ClienteConsignatario = encomienda.ClienteConsignatario == null ? null : new ClienteDto
            {
                Id = encomienda.ClienteConsignatario.Id,
                NombreCompleto = encomienda.ClienteConsignatario.NombreCompleto,
                Ci = encomienda.ClienteConsignatario.Ci,
                Telefono = encomienda.ClienteConsignatario.Telefono,
                Estado = encomienda.ClienteConsignatario.Estado
            },
            Usuario = encomienda.Usuario == null ? null : new UsuarioDto
            {
                Id = encomienda.Usuario.Id,
                Usuario = encomienda.Usuario.Usuario1,
                PuntoVentaId = encomienda.Usuario.PuntoVentaId,
                RolId = encomienda.Usuario.RolId
            }
        };

        return Ok(new { mensaje = "Encomienda actualizada correctamente", encomienda = result });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { mensaje = "Error interno procesando la encomienda.", detalle = ex.Message });
    }
}





    }
    
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.Responses;
using System.Text.Json;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasajesController : ControllerBase
    {
        private readonly TransporteContext _context;

        public PasajesController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/pasajes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PasajeListDto>>> GetPasajes()
        {
            var list = await _context.Pasajes
                .Include(p => p.Cliente)
                .Include(p => p.Asiento)
                .Include(p => p.Horario).ThenInclude(h => h.Vehiculo).ThenInclude(v => v.Conductor)
                .Include(p => p.Usuario)
                .Include(p => p.UsuarioAnula)
                .ToListAsync();

            var result = list.Select(p => new PasajeListDto
            {
                Id = p.Id,
                FechaHora = p.FechaHora,
                Monto = p.Monto,
                Movil = p.Movil,
                Estado = p.Estado,
                Destino = p.Destino,
                Asiento = p.Asiento == null ? null : new AsientoDto
                {
                    Id = p.Asiento.Id,
                    Fila = p.Asiento.Fila,
                    Columna = p.Asiento.Columna,
                    Estado = p.Asiento.Estado,
                    Numero = p.Asiento.Numero
                },
                Cliente = p.Cliente == null ? null : new ClienteDto
                {
                    Id = p.Cliente.Id,
                    NombreCompleto = p.Cliente.NombreCompleto,
                    Ci = p.Cliente.Ci,
                    Telefono = p.Cliente.Telefono,
                    Estado = p.Cliente.Estado
                },
                Usuario = p.Usuario == null ? null : new UsuarioDto
                {
                    Id = p.Usuario.Id,
                    Usuario = p.Usuario.Usuario1,
                    PuntoVentaId = p.Usuario.PuntoVentaId,
                    RolId = p.Usuario.RolId
                },
                UsuarioAnula = p.UsuarioAnula == null ? null : new UsuarioDto
                {
                    Id = p.UsuarioAnula.Id,
                    Usuario = p.UsuarioAnula.Usuario1,
                    PuntoVentaId = p.UsuarioAnula.PuntoVentaId,
                    RolId = p.UsuarioAnula.RolId
                }
            }).ToList();

            return Ok(result);
        }

        // GET: api/pasajes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PasajeListDto>> GetPasaje(int id)
        {
            var p = await _context.Pasajes
                .Include(p => p.Cliente)
                .Include(p => p.Asiento)
                .Include(p => p.Horario).ThenInclude(h => h.Vehiculo).ThenInclude(v => v.Conductor)
                .Include(p => p.Usuario)
                .Include(p => p.UsuarioAnula)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return NotFound();

            var dto = new PasajeListDto
            {
                Id = p.Id,
                FechaHora = p.FechaHora,
                Monto = p.Monto,
                Movil = p.Movil,
                Estado = p.Estado,
                Destino = p.Destino,
                Asiento = p.Asiento == null ? null : new AsientoDto
                {
                    Id = p.Asiento.Id,
                    Fila = p.Asiento.Fila,
                    Columna = p.Asiento.Columna,
                    Estado = p.Asiento.Estado,
                    Numero = p.Asiento.Numero
                },
                Cliente = p.Cliente == null ? null : new ClienteDto
                {
                    Id = p.Cliente.Id,
                    NombreCompleto = p.Cliente.NombreCompleto,
                    Ci = p.Cliente.Ci,
                    Telefono = p.Cliente.Telefono,
                    Estado = p.Cliente.Estado
                },
                Usuario = p.Usuario == null ? null : new UsuarioDto
                {
                    Id = p.Usuario.Id,
                    Usuario = p.Usuario.Usuario1,
                    PuntoVentaId = p.Usuario.PuntoVentaId,
                    RolId = p.Usuario.RolId
                },
                UsuarioAnula = p.UsuarioAnula == null ? null : new UsuarioDto
                {
                    Id = p.UsuarioAnula.Id,
                    Usuario = p.UsuarioAnula.Usuario1,
                    PuntoVentaId = p.UsuarioAnula.PuntoVentaId,
                    RolId = p.UsuarioAnula.RolId
                }
            };

            return Ok(dto);
        }

        // POST: api/pasajes
        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult> PostPasaje([FromBody] JsonElement raw)
        {
            // Imprimir body crudo para depuración
            try
            {
                var rawText = raw.GetRawText();
                System.Console.WriteLine("----- RAW BODY RECEIVED -----");
                System.Console.WriteLine(rawText);

                // Si el cliente envía un array, procesamos cada elemento
                if (raw.ValueKind == JsonValueKind.Array)
                {
                    var results = new List<object>();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    foreach (var item in raw.EnumerateArray())
                    {
                        try
                        {
                            var itemText = item.GetRawText();
                            var dtoItem = JsonSerializer.Deserialize<PasajeCreateDto>(itemText, options);
                            if (dtoItem == null)
                            {
                                results.Add(new { ok = false, error = "No se pudo deserializar item", item = JsonDocument.Parse(itemText).RootElement });
                                continue;
                            }

                            // Manejar cliente
                            int? clienteId = null;
                            if (dtoItem.Cliente != null)
                            {
                                if (dtoItem.Cliente.Id.HasValue && dtoItem.Cliente.Id.Value > 0)
                                {
                                    var existing = await _context.Clientes.FindAsync(dtoItem.Cliente.Id.Value);
                                    if (existing == null)
                                    {
                                        results.Add(new { ok = false, error = "Cliente no encontrado", clienteId = dtoItem.Cliente.Id });
                                        continue;
                                    }
                                    clienteId = existing.Id;
                                }
                                else
                                {
                                    var newCliente = new Cliente
                                    {
                                        NombreCompleto = dtoItem.Cliente.NombreCompleto,
                                        Ci = dtoItem.Cliente.Ci,
                                        Telefono = dtoItem.Cliente.Telefono,
                                        Estado = dtoItem.Cliente.Estado
                                    };
                                    _context.Clientes.Add(newCliente);
                                    await _context.SaveChangesAsync();
                                    clienteId = newCliente.Id;
                                }
                            }

                            var pasajeItem = new Pasaje
                            {
                                FechaHora = dtoItem.FechaHora,
                                Monto = dtoItem.Monto,
                                Movil = dtoItem.Movil,
                                Estado = dtoItem.Estado,
                                Destino = dtoItem.Destino,
                                AsientoId = dtoItem.AsientoId,
                                HorarioId = dtoItem.HorarioId,
                                UsuarioId = dtoItem.UsuarioId,
                                ClienteId = clienteId
                            };

                            _context.Pasajes.Add(pasajeItem);
                            await _context.SaveChangesAsync();

                            results.Add(new { ok = true, pasajeId = pasajeItem.Id, clienteId = clienteId });
                        }
                        catch (Exception exItem)
                        {
                            results.Add(new { ok = false, error = exItem.Message });
                        }
                    }

                    return Ok(new { mensaje = "Procesado array", resultados = results });
                }

                // Si es un objeto, intentamos deserializar a PasajeCreateDto y procesarlo
                if (raw.ValueKind == JsonValueKind.Object)
                {
                    var dto = JsonSerializer.Deserialize<PasajeCreateDto>(rawText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dto == null)
                    {
                        return BadRequest(new { mensaje = "No se pudo deserializar a PasajeCreateDto." });
                    }

                    // Validación mínima
                    if (!TryValidateModel(dto))
                    {
                        return BadRequest(ModelState);
                    }

                    // Reusar la lógica existente para crear cliente y pasaje
                    int? clienteId = null;
                    if (dto.Cliente != null)
                    {
                        if (dto.Cliente.Id.HasValue && dto.Cliente.Id.Value > 0)
                        {
                            var existing = await _context.Clientes.FindAsync(dto.Cliente.Id.Value);
                            if (existing == null) return BadRequest("Cliente no encontrado");
                            clienteId = existing.Id;
                        }
                        else
                        {
                            var newCliente = new Cliente
                            {
                                NombreCompleto = dto.Cliente.NombreCompleto,
                                Ci = dto.Cliente.Ci,
                                Telefono = dto.Cliente.Telefono,
                                Estado = dto.Cliente.Estado
                            };
                            _context.Clientes.Add(newCliente);
                            await _context.SaveChangesAsync();
                            clienteId = newCliente.Id;
                        }
                    }

                    var pasaje = new Pasaje
                    {
                        FechaHora = dto.FechaHora,
                        Monto = dto.Monto,
                        Movil = dto.Movil,
                        Estado = dto.Estado,
                        Destino = dto.Destino,
                        AsientoId = dto.AsientoId,
                        HorarioId = dto.HorarioId,
                        UsuarioId = dto.UsuarioId,
                        ClienteId = clienteId
                    };

                    _context.Pasajes.Add(pasaje);
                    await _context.SaveChangesAsync();

                    return CreatedAtAction(nameof(GetPasaje), new { id = pasaje.Id }, pasaje);
                }

                return BadRequest(new { mensaje = "JSON recibido no es un objeto ni un array válido." });
            }
            catch (JsonException jex)
            {
                return BadRequest(new { mensaje = "JSON inválido.", detalle = jex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = "Error interno procesando el JSON.", detalle = ex.Message });
            }
        }

        // PUT: api/pasajes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPasaje(int id, Pasaje pasaje)
        {
            if (id != pasaje.Id) return BadRequest();

            _context.Entry(pasaje).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Pasajes.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return Ok(pasaje);
        }

        // DELETE: api/pasajes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePasaje(int id)
        {

            var pasaje = await _context.Pasajes.FindAsync(id);
            if (pasaje == null) return NotFound();

            pasaje.Estado = false; // marcar como inactivo en lugar de eliminar
            pasaje.UsuarioAnulaId = /* aquí asignas el usuario que anula */ null;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "eliminado" });
        }
    }
}

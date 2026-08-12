using System.Collections.Generic;

namespace backend.Models.Responses
{
    public class ClienteDto
    {
        public int Id { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Ci { get; set; }
        public string? Telefono { get; set; }
        public bool? Estado { get; set; }
    }

    public class AsientoDto
    {
        public int Id { get; set; }
        public int? Fila { get; set; }
        public int? Columna { get; set; }
        public bool? Estado { get; set; }
        public int? Numero { get; set; }
        // Puede venir desde el frontend (distribucion_id) o usarse internamente
        public int? DistribucionId { get; set; }
        public List<PasajeSummaryDto>? Pasajes { get; set; }
    }

    public class PasajeListDto
    {
        public int Id { get; set; }
        public string? FechaHora { get; set; }
        public int? Monto { get; set; }
        public string? Movil { get; set; }
        public bool? Estado { get; set; }
        public string? Destino { get; set; }
        public bool? Reserva { get; set; }
        public AsientoDto? Asiento { get; set; }
        public ClienteDto? Cliente { get; set; }
        public UsuarioDto? Usuario { get; set; }
        public UsuarioDto? UsuarioAnula { get; set; }
    }

    public class PasajeSummaryDto
    {
        public int Id { get; set; }
        public string? FechaHora { get; set; }
        public int? Monto { get; set; }
        public bool? Estado { get; set; }
        public ClienteDto? Cliente { get; set; }
    }

    public class ClienteCreateDto
    {
        public int? Id { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Ci { get; set; }
        public string? Telefono { get; set; }
        public bool? Estado { get; set; }
    }

    public class PasajeCreateDto
    {
        public string? FechaHora { get; set; }
        public int? Monto { get; set; }
        public string? Movil { get; set; }
        public bool? Reserva { get; set; }
        public bool? Estado { get; set; }
        public string? Destino { get; set; }
        public int? AsientoId { get; set; }
        public int? HorarioId { get; set; }
        public int? UsuarioId { get; set; }
        public ClienteCreateDto? Cliente { get; set; }
    }

    public class EncomiendaListDto
    {
        public int Id { get; set; }
        public string? Contenido { get; set; }
        public string? FechaRecepcion { get; set; }
        public string? FechaEntrega { get; set; }
        public double? Monto { get; set; }
        public string? Numero { get; set; }
        public string? Destino { get; set; }
        public bool? Estado { get; set; }
        public bool? Pagado { get; set; }
        public ClienteDto? ClienteRemitente { get; set; }
        public ClienteDto? ClienteConsignatario { get; set; }
        public UsuarioDto? Usuario { get; set; }
    }

    public class UsuarioDto
    {
        public int Id { get; set; }
        public string? Usuario { get; set; }
        public int? PuntoVentaId { get; set; }
        public int? RolId { get; set; }
    }

    public class ConductorDto
    {
        public int Id { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? Telefono { get; set; }
    }

    public class DestinoDto
    {
        public int Id { get; set; }
        public bool? EsOrigen { get; set; }
        public int? Orden { get; set; }
        public PuntoVentaDto? PuntoVenta { get; set; }
    }

    public class PuntoVentaDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
    }

    public class RutaDto
    {
        public int Id { get; set; }
        public string? Dias { get; set; }
        public int? Tarifa { get; set; }
        public bool? Estado { get; set; }
        public List<DestinoDto>? Destinos { get; set; }
    }

    public class HorarioListDto
    {
        public int Id { get; set; }
        public string? Fecha { get; set; }
        public string? Hora { get; set; }
        public bool? Estado { get; set; }
        public RutaDto? Ruta { get; set; }
        public VehiculoListLiteDto? Vehiculo { get; set; }
        public List<PasajeListDto>? Pasajes { get; set; }
    }

    public class DistribucionDto
    {
        public int Id { get; set; }
        public bool? Estado { get; set; }
        public string? Nombre { get; set; }
        // Metadato no persistido en el modelo, aceptado desde frontend
        public List<AsientoDto>? Asientos { get; set; }
    }

    public class VehiculoListLiteDto
    {
        public int Id { get; set; }
        public string? Placa { get; set; }
        public string? Movil { get; set; }
        public ConductorDto? Conductor { get; set; }
        public DistribucionDto? Distribucion { get; set; }
        public static implicit operator VehiculoListLiteDto?(VehiculoLiteDto? v)
        {
            throw new NotImplementedException();
        }
    }
    public class VehiculoLiteDto
    {
        public int Id { get; set; }
        public string? Placa { get; set; }
        public string? Movil { get; set; }
        public ConductorDto? Conductor { get; set; }
        public DistribucionDto? Distribucion { get; set; }
    }

    public class VehiculoListDto
    {
        public int Id { get; set; }
        public string? Movil { get; set; }
        public string? Placa { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public bool? Estado { get; set; }
        public string? Color { get; set; }
        public string? Tipo { get; set; }
        public string? Soat { get; set; }
        public string? Aseguradora { get; set; }
        public ConductorDto? Conductor { get; set; }
        public ConductorDto? Propietario { get; set; }
        public DistribucionDto? Distribucion { get; set; }
    }
}

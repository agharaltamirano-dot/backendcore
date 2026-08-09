using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Encomiendum
{
    public int Id { get; set; }

    public string? Contenido { get; set; }

    public bool? Estado { get; set; }

    public string? FechaRecepcion { get; set; }

    public string? FechaEntrega { get; set; }

    public double? Monto { get; set; }

    public string? Numero { get; set; }

    public int? UsuarioId { get; set; }

    public int? ClienteRemitenteId { get; set; }

    public int? ClienteConsignatarioId { get; set; }

    public int? UsuarioAnulaId { get; set; }

    public virtual Cliente? ClienteConsignatario { get; set; }

    public virtual Cliente? ClienteRemitente { get; set; }

    public virtual ICollection<Envio> Envios { get; set; } = new List<Envio>();

    public virtual Usuario? Usuario { get; set; }

    public virtual Usuario? UsuarioAnula { get; set; }
}

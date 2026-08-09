using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Usuario1 { get; set; }

    public string? Clave { get; set; }

    public bool? Estado { get; set; }

    public string? UltimoAcceso { get; set; }

    public int? RolId { get; set; }

    public bool? Acceso { get; set; }

    public int? PuntoVentaId { get; set; }

    public virtual ICollection<Encomiendum> EncomiendumUsuarioAnulas { get; set; } = new List<Encomiendum>();

    public virtual ICollection<Encomiendum> EncomiendumUsuarios { get; set; } = new List<Encomiendum>();

    public virtual ICollection<Pasaje> PasajeUsuarioAnulas { get; set; } = new List<Pasaje>();

    public virtual ICollection<Pasaje> PasajeUsuarios { get; set; } = new List<Pasaje>();

    public virtual PuntoVentum? PuntoVenta { get; set; }

    public virtual Rol? Rol { get; set; }
}

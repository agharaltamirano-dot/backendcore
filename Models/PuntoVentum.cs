using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class PuntoVentum
{
    public int Id { get; set; }

    public string? Direccion { get; set; }

    public string? Nombre { get; set; }

    public string? Telefono { get; set; }

    public bool? VisiblePasajes { get; set; }

    public bool? EsPuntoVenta { get; set; }

    public virtual ICollection<Rutum> RutumDestinos { get; set; } = new List<Rutum>();

    public virtual ICollection<Rutum> RutumOrigens { get; set; } = new List<Rutum>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Menu
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int? PadreId { get; set; }

    public string Tipo { get; set; } = null!;

    public string? RutaAccion { get; set; }

    public string? Icono { get; set; }

    public int? Orden { get; set; }

    public virtual ICollection<Menu> InversePadre { get; set; } = new List<Menu>();

    public virtual Menu? Padre { get; set; }

    public virtual ICollection<Rol> Rols { get; set; } = new List<Rol>();
}

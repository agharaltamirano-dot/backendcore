using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Cliente
{
    public int Id { get; set; }

    public string? NombreCompleto { get; set; }

    public string? Ci { get; set; }

    public bool? Estado { get; set; }

    public string? Telefono { get; set; }

    public virtual ICollection<Encomiendum> EncomiendumClienteConsignatarios { get; set; } = new List<Encomiendum>();

    public virtual ICollection<Encomiendum> EncomiendumClienteRemitentes { get; set; } = new List<Encomiendum>();

    public virtual ICollection<Pasaje> Pasajes { get; set; } = new List<Pasaje>();
}

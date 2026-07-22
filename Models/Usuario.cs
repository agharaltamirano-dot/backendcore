using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Usuario1 { get; set; }

    public string? Clave { get; set; }

    public bool? Estado { get; set; }

    public bool? Acceso { get; set; }

    public string? UltimoAcceso { get; set; }

    public int? RolId { get; set; }

    public virtual Rol? Rol { get; set; }
}

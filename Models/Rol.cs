using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Rol
{
    public int Id { get; set; }

    public string? Nombre { get; set; }

    public bool? Estado { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
}

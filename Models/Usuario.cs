using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Nombre { get; set; }

    public string? Clave { get; set; }

    public bool? Estado {get; set; }

}

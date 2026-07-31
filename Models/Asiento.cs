using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Asiento
{
    public int Id { get; set; }

    public int? Filas { get; set; }

    public int? Cantidad { get; set; }

    public bool? Estado { get; set; }

    public bool? Esminibus { get; set; }

    public string? Nombre { get; set; }

    public virtual ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}

using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class DistribucionAsiento
{
    public int Id { get; set; }

    public bool? Estado { get; set; }

    public string? Nombre { get; set; }

    public virtual ICollection<Asiento> Asientos { get; set; } = new List<Asiento>();

    public virtual ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}

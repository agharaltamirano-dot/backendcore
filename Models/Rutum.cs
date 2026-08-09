using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Rutum
{
    public int Id { get; set; }

    public string? Dias { get; set; }

    public bool? Estado { get; set; }

    public int? Tarifa { get; set; }

    public virtual ICollection<Destino> Destinos { get; set; } = new List<Destino>();

    public virtual ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}

using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Vehiculo
{
    public int Id { get; set; }

    public string? Movil { get; set; }

    public string? Placa { get; set; }

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? Color { get; set; }

    public string? Tipo { get; set; }

    public string? Soat { get; set; }

    public string? Aseguradora { get; set; }

    public int? ConductorId { get; set; }

    public int? PropietarioId { get; set; }

    public bool? Estado { get; set; }

    public int? AsientosId { get; set; }

    public virtual Asiento? Asientos { get; set; }

    public virtual Conductor? Conductor { get; set; }

    public virtual ICollection<Horario> Horarios { get; set; } = new List<Horario>();

    public virtual Conductor? Propietario { get; set; }
}

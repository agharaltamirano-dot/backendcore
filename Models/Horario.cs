using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Horario
{
    public int Id { get; set; }

    public bool? Estado { get; set; }

    public string? Fecha { get; set; }

    public string? Hora { get; set; }

    public int? RutaId { get; set; }

    public int? VehiculoId { get; set; }

    public virtual Rutum? Ruta { get; set; }

    public virtual Vehiculo? Vehiculo { get; set; }
}

using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Conductor
{
    public int Id { get; set; }

    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public string? Telefono { get; set; }

    public string? Licencia { get; set; }

    public string? Categoria { get; set; }

    public bool? Estado { get; set; }

    public string? FotoLicencia { get; set; }

    public virtual ICollection<Envio> Envios { get; set; } = new List<Envio>();

    public virtual ICollection<Vehiculo> VehiculoConductors { get; set; } = new List<Vehiculo>();

    public virtual ICollection<Vehiculo> VehiculoPropietarios { get; set; } = new List<Vehiculo>();
}

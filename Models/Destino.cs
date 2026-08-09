using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Destino
{
    public int Id { get; set; }

    public bool? EsOrigen { get; set; }

    public int? PuntoVentaId { get; set; }

    public int? RutaId { get; set; }

    public int? Orden { get; set; }

    public virtual PuntoVentum? PuntoVenta { get; set; }

    public virtual Rutum? Ruta { get; set; }
}

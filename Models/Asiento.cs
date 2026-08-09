using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Asiento
{
    public int Id { get; set; }

    public int? Fila { get; set; }

    public int? Columna { get; set; }

    public int? DistribucionId { get; set; }

    public bool? Estado { get; set; }

    public int? Numero { get; set; }

    public virtual DistribucionAsiento? Distribucion { get; set; }

    public virtual ICollection<Pasaje> Pasajes { get; set; } = new List<Pasaje>();
}

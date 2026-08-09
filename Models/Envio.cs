using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Envio
{
    public int Id { get; set; }

    public int? ConductorId { get; set; }

    public int? EncomiendaId { get; set; }

    public string? Fecha { get; set; }

    public int? HorarioId { get; set; }

    public virtual Conductor? Conductor { get; set; }

    public virtual Encomiendum? Encomienda { get; set; }

    public virtual Horario? Horario { get; set; }
}

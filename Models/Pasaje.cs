using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class Pasaje
{
    public int Id { get; set; }

    public int? ClienteId { get; set; }

    public bool? Estado { get; set; }

    public string? FechaHora { get; set; }

    public int? Monto { get; set; }

    public string? Movil { get; set; }

    public int? UsuarioId { get; set; }

    public int? HorarioId { get; set; }

    public string? Destino { get; set; }

    public int? AsientoId { get; set; }

    public int? UsuarioAnulaId { get; set; }

    public bool? Reserva { get; set; }

    public virtual Asiento? Asiento { get; set; }

    public virtual Cliente? Cliente { get; set; }

    public virtual Horario? Horario { get; set; }

    public virtual Usuario? Usuario { get; set; }

    public virtual Usuario? UsuarioAnula { get; set; }
}

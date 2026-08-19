using backend.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Globalization;

namespace backend.Controllers.reportes;

[ApiController]
[Route("api/[controller]")]
public class reporteHorarioController : ControllerBase
{
    private readonly TransporteContext _context;
    public reporteHorarioController(TransporteContext context) => _context = context;

    [HttpGet("reporte-horarios/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> Pdf([FromQuery] string? fechaInicio, [FromQuery] string? fechaFin, [FromQuery] int? destinoId, [FromQuery] int? horarioId, [FromQuery] bool? estado, [FromQuery] string? nombreUsuario)
    {
        var r = await Consultar(fechaInicio, fechaFin, destinoId, horarioId, estado); if (r.Error != null) return BadRequest(r.Error);
        using var doc = new PdfDocument(); XGraphics? g = null; double y = 0, w = 0, h = 0; const double m = 28;
        var title = new XFont("Arial", 15, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode)); var meta = new XFont("Arial", 8, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode)); var head = new XFont("Arial", 7, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode)); var body = new XFont("Arial", 6.5, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode)); var logo = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo9.png");
        void Header()
{
    var x = m;
    foreach (var c in Cols())
    {
        g!.DrawRectangle(new XSolidBrush(XColor.FromKnownColor(XKnownColor.LightBlue)), x, y, c.W, 16);
        g.DrawString(c.N, head, XBrushes.Black,
            new XRect(x + 2, y + 3, c.W - 4, 10), XStringFormats.TopLeft);
        x += c.W;
    }
    y += 16;
}

        void Page(int pageNumber, int totalPages)
{
    g?.Dispose();
    var p = doc.AddPage();
    p.Size = PdfSharpCore.PageSize.Letter;
    p.Orientation = PdfSharpCore.PageOrientation.Landscape;
    g = XGraphics.FromPdfPage(p);
    w = p.Width.Point;
    h = p.Height.Point;
    y = m;

    // Logo primero
    if (System.IO.File.Exists(logo))
    {
        using var img = XImage.FromFile(logo);
        g.DrawImage(img, m, y, 70, 50); // logo a la izquierda
    }

    // Título al lado del logo
    g.DrawString("Reporte detallado de horarios", title, XBrushes.Black,
        new XRect(m + 80, y + 10, 400, 22), XStringFormats.TopLeft);

    // Datos de usuario y fecha
    g.DrawString($"Generado por: {nombreUsuario ?? "No especificado"}", meta, XBrushes.Black,
        new XRect(w - m - 250, y, 250, 12), XStringFormats.TopRight);
    g.DrawString($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}", meta, XBrushes.Black,
        new XRect(w - m - 250, y + 11, 250, 12), XStringFormats.TopRight);

    // Número de página (si hay varias)
    g.DrawString($"Página {pageNumber} de {totalPages}", meta, XBrushes.Black,
        new XRect(w - m - 250, y + 22, 250, 12), XStringFormats.TopRight);

    y += 60; // espacio después del encabezado
    g.DrawString(r.Filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", r.Filtros),
        meta, XBrushes.Black, new XRect(m, y, w - m * 2, 28), XStringFormats.TopLeft);

    y += 30;
    Header();
}

        int pageNumber = 1;
int totalPages = (int)Math.Ceiling((double)r.Horarios.Count / 20.0); // ejemplo: 20 filas por página

Page(pageNumber, totalPages);

foreach (var horario in r.Horarios)
{
    if (y + 28 > h - m)
    {
        pageNumber++;
        Page(pageNumber, totalPages);
    }

    var x = m;
    var v = Values(horario);
    foreach (var c in Cols())
{
    g!.DrawRectangle(XPens.LightGray, x, y, c.W, 28);

    // Color dinámico para la columna Estado
    var brush = XBrushes.Black;
    if (c.N == "Estado")
        brush = horario.Estado == true ? XBrushes.Green : XBrushes.Red;

    g.DrawString(v[c.I], body, brush,
        new XRect(x + 2, y + 2, c.W - 4, 24), XStringFormats.TopLeft);
    x += c.W;
}

    y += 28;
}
 g?.Dispose(); using var ms = new MemoryStream(); doc.Save(ms, false); Response.Headers["Content-Disposition"] = "inline; filename=\"reporte_horarios.pdf\""; return File(ms.ToArray(), "application/pdf");
    }

    [HttpGet("reporte-horarios/xlsx")]
    [AllowAnonymous]
    public async Task<IActionResult> Xlsx([FromQuery] string? fechaInicio, [FromQuery] string? fechaFin, [FromQuery] int? destinoId, [FromQuery] int? horarioId, [FromQuery] bool? estado, [FromQuery] string? nombreUsuario)
    {
        var r = await Consultar(fechaInicio, fechaFin, destinoId, horarioId, estado); if (r.Error != null) return BadRequest(r.Error); using var book = new XLWorkbook(); var s = book.Worksheets.Add("Horarios"); ExcelHeader(s, nombreUsuario, r.Filtros); var headers = new[] { "ID", "Fecha", "Hora", "Estado", "Ruta", "Destinos", "Vehículo", "Conductor", "Encomiendas activas", "Total pasajes activos (Bs)" }; for (var i = 0; i < headers.Length; i++) s.Cell(7, i + 1).Value = headers[i]; var row = 8; foreach (var h in r.Horarios) { var v = Values(h); for (var i = 0; i < v.Length; i++) s.Cell(row, i + 1).Value = v[i]; row++; } ExcelEnd(s, row, headers.Length); using var ms = new MemoryStream(); book.SaveAs(ms); return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reporte_horarios.xlsx");
    }

    private async Task<Result> Consultar(string? inicio, string? fin, int? destinoId, int? horarioId, bool? estado)
    {
        if (new[] { inicio, fin }.Any(x => x != null && !DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))) return new Result { Error = "Las fechas deben tener el formato yyyy-MM-dd." };
        var q = _context.Horarios.AsNoTracking().Include(h => h.Ruta).ThenInclude(r => r.Destinos).ThenInclude(d => d.PuntoVenta).Include(h => h.Vehiculo).ThenInclude(v => v.Conductor).Include(h => h.Pasajes).Include(h => h.Envios).ThenInclude(e => e.Encomienda).AsQueryable();
        if (horarioId.HasValue) q = q.Where(h => h.Id == horarioId); if (estado.HasValue) q = q.Where(h => h.Estado == estado); if (destinoId.HasValue) q = q.Where(h => h.Ruta != null && h.Ruta.Destinos.Any(d => d.Id == destinoId)); if (inicio != null) q = q.Where(h => h.Fecha != null && string.Compare(h.Fecha, inicio) >= 0); if (fin != null) q = q.Where(h => h.Fecha != null && string.Compare(h.Fecha, fin) <= 0);
        var f = new List<string>(); if (inicio != null) f.Add($"Fecha inicio: {inicio}"); if (fin != null) f.Add($"Fecha fin: {fin}"); if (destinoId.HasValue) f.Add($"Destino ID: {destinoId}"); if (horarioId.HasValue) f.Add($"Horario ID: {horarioId}"); if (estado.HasValue) f.Add($"Estado: {(estado.Value ? "Activo" : "Inactivo")}"); return new Result { Horarios = await q.OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Hora).ToListAsync(), Filtros = f };
    }
    private static string[] Values(Horario h)
{
    var destinos = h.Ruta?.Destinos
        .OrderBy(d => d.Orden)
        .Select(d => d.PuntoVenta?.Nombre ?? $"Destino {d.Id}") ?? [];

    var totalEncomiendas = h.Envios
        .Where(e => e.Encomienda?.Estado == true)
        .Sum(e => e.Encomienda?.Monto ?? 0);

    var totalPasajes = h.Pasajes
        .Where(p => p.Estado == true)
        .Sum(p => p.Monto ?? 0);

    return [
        h.Id.ToString(),
        h.Fecha ?? "",
        h.Hora ?? "",
        h.Estado == true ? "Activo" : "Inactivo",
        h.Ruta?.Id.ToString() ?? "",
        string.Join(" → ", destinos),
        h.Vehiculo?.Movil ?? h.Vehiculo?.Placa ?? "",
        $"{h.Vehiculo?.Conductor?.Nombres} {h.Vehiculo?.Conductor?.Apellidos}".Trim(),
        totalEncomiendas.ToString("N2", CultureInfo.InvariantCulture),
        totalPasajes.ToString("N2", CultureInfo.InvariantCulture)
    ];
}

    private static (string N, double W, int I)[] Cols() => [("ID", 30, 0), ("Fecha", 55, 1), ("Hora", 38, 2), ("Estado", 45, 3), ("Ruta", 35, 4), ("Destinos", 165, 5), ("Vehículo", 58, 6), ("Conductor", 105, 7), ("Encomiendas Bs", 60, 8), ("Pasajes Bs", 70, 9)];
    private static void ExcelHeader(IXLWorksheet s, string? usuario, List<string> filtros) { s.Range("A1:F2").Merge(); s.Cell("A1").Value = "Reporte detallado de horarios"; s.Cell("A1").Style.Font.Bold = true; s.Cell("A1").Style.Font.FontSize = 16; var p = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo9.png"); if (System.IO.File.Exists(p)) s.AddPicture(p).MoveTo(s.Cell("J1")).WithSize(110, 70); s.Range("H3:J3").Merge(); s.Range("H4:J4").Merge(); s.Cell("H3").Value = $"Generado por: {usuario ?? "No especificado"}"; s.Cell("H4").Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}"; s.Range("H3:J4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; s.Range("A5:J5").Merge(); s.Cell("A5").Value = filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", filtros); s.Cell("A5").Style.Alignment.WrapText = true; }
    private static void ExcelEnd(IXLWorksheet s, int row, int cols) { var r = s.Range(7, 1, 7, cols); r.Style.Font.Bold = true; r.Style.Fill.BackgroundColor = XLColor.LightGray; if (row > 8) s.Range(7, 1, row - 1, cols).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; s.Columns(1, cols).AdjustToContents(); s.SheetView.FreezeRows(7); }
    private sealed class Result { public List<Horario> Horarios { get; set; } = []; public List<string> Filtros { get; set; } = []; public string? Error { get; set; } }
}

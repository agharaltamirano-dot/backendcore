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
public class reporteConductorController : ControllerBase
{
    private readonly TransporteContext _context;
    public reporteConductorController(TransporteContext context) => _context = context;

    [HttpGet("{conductorId}/pdf")]
[AllowAnonymous]
public async Task<IActionResult> Pdf(int conductorId, [FromQuery] string? fechaInicio, [FromQuery] string? fechaFin, [FromQuery] int? destinoId, [FromQuery] int? horarioId, [FromQuery] string? nombreUsuario)
{
    var r = await Consultar(conductorId, fechaInicio, fechaFin, destinoId, horarioId);
    if (r.Error != null) return BadRequest(r.Error);
    if (r.Conductor == null) return NotFound("Conductor no encontrado.");

    using var doc = new PdfDocument();
    XGraphics? g = null;
    double y = 0, w = 0, h = 0;
    const double m = 28;

    var title = new XFont("Arial", 15, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
    var meta = new XFont("Arial", 8, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
    var sub = new XFont("Arial", 10, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
    var head = new XFont("Arial", 7, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
    var body = new XFont("Arial", 6.5, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
    var logo = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg");

    void TableHeader()
    {
        var x = m;
        foreach (var c in Cols())
        {
            g!.DrawRectangle(XBrushes.LightGray, x, y, c.W, 16);
            g.DrawString(c.N, head, XBrushes.Black, new XRect(x + 2, y + 3, c.W - 4, 10), XStringFormats.TopLeft);
            x += c.W;
        }
        y += 16;
    }

    void Page()
    {
        g?.Dispose();
        var p = doc.AddPage();
        p.Size = PdfSharpCore.PageSize.Letter;
        p.Orientation = PdfSharpCore.PageOrientation.Landscape;
        g = XGraphics.FromPdfPage(p);
        w = p.Width.Point;
        h = p.Height.Point;
        y = m;

        // Logo a la izquierda
        if (System.IO.File.Exists(logo))
        {
            using var img = XImage.FromFile(logo);
            g.DrawImage(img, m, y, 70, 50);
        }

        // Título al lado del logo
        g.DrawString($"Reporte de encomiendas - {r.Conductor.Nombres} {r.Conductor.Apellidos}",
            title, XBrushes.Black, new XRect(m + 80, y + 10, 400, 22), XStringFormats.TopLeft);

        // Datos de usuario y fecha a la derecha
        g.DrawString($"Generado por: {nombreUsuario ?? "No especificado"}", meta, XBrushes.Black,
            new XRect(w - m - 250, y, 250, 12), XStringFormats.TopRight);
        g.DrawString($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}", meta, XBrushes.Black,
            new XRect(w - m - 250, y + 11, 250, 12), XStringFormats.TopRight);

        y += 60;
        g.DrawString(r.Filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", r.Filtros),
            meta, XBrushes.Black, new XRect(m, y, w - m * 2, 28), XStringFormats.TopLeft);

        y += 30;
    }

    void ConductorHeader()
    {
        if (y + 36 > h - m) Page();
        g!.DrawString($"Conductor: {r.Conductor.Nombres} {r.Conductor.Apellidos} | Teléfono: {r.Conductor.Telefono ?? ""} | Licencia: {r.Conductor.Licencia ?? ""}",
            sub, XBrushes.Black, new XRect(m, y, w - 2 * m, 16), XStringFormats.TopLeft);
        y += 18;
        TableHeader();
    }

    Page();
    ConductorHeader();

    foreach (var envio in r.Envios)
    {
        if (y + 22 > h - m) { Page(); ConductorHeader(); }
        var x = m;
        var v = Values(envio);

        foreach (var c in Cols())
        {
            g!.DrawRectangle(XPens.LightGray, x, y, c.W, 22);

            // Si envio == null, dejar vacío en Fecha Envío
            string texto = v[c.I];
            if (c.N == "Fecha Envío" && envio.Encomienda == null)
                texto = "";

            // Columna Horario vacía de momento
            if (c.N == "Horario")
                texto = "";

            // Usar XTextFormatter para salto de línea
            var rect = new XRect(x + 2, y + 2, c.W - 4, 18);
            var tf = new PdfSharpCore.Drawing.Layout.XTextFormatter(g);
            tf.DrawString(texto, body, XBrushes.Black, rect, XStringFormats.TopLeft);

            x += c.W;
        }
        y += 22;
    }

    var total = r.Envios.Sum(e => e.Encomienda?.Monto ?? 0);
    var pagado = r.Envios.Where(e => e.Encomienda?.Pagado == true).Sum(e => e.Encomienda?.Monto ?? 0);
    var pendiente = r.Envios.Where(e => e.Encomienda?.Pagado == false).Sum(e => e.Encomienda?.Monto ?? 0);

    if (y + 18 > h - m) Page();
    g!.DrawString($"Resumen: Total Bs {total:N2} | Pagado Bs {pagado:N2} | Pendiente Bs {pendiente:N2}",
        head, XBrushes.Black, new XRect(m, y + 3, w - 2 * m, 14), XStringFormats.TopLeft);

    g?.Dispose();
    using var ms = new MemoryStream();
    doc.Save(ms, false);
    Response.Headers["Content-Disposition"] = $"inline; filename=\"reporte_conductor_{conductorId}.pdf\"";
    return File(ms.ToArray(), "application/pdf");
}


    [HttpGet("{conductorId}/xlsx")]
    [AllowAnonymous]
    public async Task<IActionResult> Xlsx(int conductorId, [FromQuery] string? fechaInicio, [FromQuery] string? fechaFin, [FromQuery] int? destinoId, [FromQuery] int? horarioId, [FromQuery] string? nombreUsuario)
    {
        var r = await Consultar(conductorId, fechaInicio, fechaFin, destinoId, horarioId); 
        if (r.Error != null) return BadRequest(r.Error); 
        if (r.Conductor == null) return NotFound("Conductor no encontrado.");
        
        using var book = new XLWorkbook(); 
        var s = book.Worksheets.Add($"Conductor {r.Conductor.Nombres}"); 
        ExcelHeader(s, nombreUsuario, r.Filtros, r.Conductor); 
        
        var headers = new[] { "Fecha envío", "Horario", "Nro. encomienda", "Recepción", "Entrega", "Destino", "Contenido", "Monto (Bs)", "Estado", "Pagado" }; 
        for (var i = 0; i < headers.Length; i++) s.Cell(7, i + 1).Value = headers[i]; 
        
        var row = 8; 
        foreach (var e in r.Envios) 
        { 
            var v = ValuesWithoutConductor(e); 
            for (var i = 0; i < v.Length; i++) s.Cell(row, i + 1).Value = v[i]; 
            row++; 
        }
        
        var total = r.Envios.Sum(e => e.Encomienda?.Monto ?? 0); 
        var pagado = r.Envios.Where(e => e.Encomienda?.Pagado == true).Sum(e => e.Encomienda?.Monto ?? 0); 
        var pendiente = r.Envios.Where(e => e.Encomienda?.Pagado == false).Sum(e => e.Encomienda?.Monto ?? 0); 
        s.Range(row, 1, row, headers.Length).Merge(); 
        s.Cell(row, 1).Value = $"Resumen: Total Bs {total:N2} | Pagado Bs {pagado:N2} | Pendiente Bs {pendiente:N2}"; 
        s.Cell(row, 1).Style.Font.Bold = true; 
        s.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray; 
        
        ExcelEnd(s, row, headers.Length); 
        using var ms = new MemoryStream(); 
        book.SaveAs(ms); 
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"reporte_conductor_{conductorId}.xlsx");
    }

    private async Task<Result> Consultar(int conductorId, string? inicio, string? fin, int? destinoId, int? horarioId)
    {
        if (new[] { inicio, fin }.Any(x => x != null && !DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))) 
            return new Result { Error = "Las fechas deben tener el formato yyyy-MM-dd." };
        
        var conductor = await _context.Conductors.AsNoTracking().FirstOrDefaultAsync(c => c.Id == conductorId);
        if (conductor == null) return new Result { Error = "Conductor no encontrado." };
        
        var q = _context.Envios.AsNoTracking()
            .Include(e => e.Conductor)
            .Include(e => e.Encomienda)
            .Include(e => e.Horario).ThenInclude(h => h.Ruta).ThenInclude(r => r.Destinos)
            .Where(e => e.ConductorId == conductorId)
            .AsQueryable(); 
        
        if (horarioId.HasValue) q = q.Where(e => e.HorarioId == horarioId); 
        if (destinoId.HasValue) q = q.Where(e => e.Horario != null && e.Horario.Ruta != null && e.Horario.Ruta.Destinos.Any(d => d.Id == destinoId)); 
        if (inicio != null) q = q.Where(e => e.Fecha != null && string.Compare(e.Fecha, inicio + " 00:00") >= 0); 
        if (fin != null) q = q.Where(e => e.Fecha != null && string.Compare(e.Fecha, fin + " 23:59:59") <= 0);
        
        var f = new List<string> { $"Conductor: {conductor.Nombres} {conductor.Apellidos}" }; 
        if (inicio != null) f.Add($"Fecha inicio: {inicio}"); 
        if (fin != null) f.Add($"Fecha fin: {fin}"); 
        if (destinoId.HasValue) f.Add($"Destino ID: {destinoId}"); 
        if (horarioId.HasValue) f.Add($"Horario ID: {horarioId}"); 
        
        return new Result 
        { 
            Conductor = conductor,
            Envios = await q.OrderByDescending(e => e.Fecha).ToListAsync(), 
            Filtros = f 
        };
    }
    private static string[] Values(Envio e) => [$"{e.Conductor?.Nombres} {e.Conductor?.Apellidos}".Trim(), e.Conductor?.Telefono ?? "", e.Conductor?.Licencia ?? "", e.Fecha ?? "", e.HorarioId?.ToString() ?? "", e.Encomienda?.Numero ?? "", e.Encomienda?.FechaRecepcion ?? "", e.Encomienda?.FechaEntrega ?? "", e.Encomienda?.Destino ?? "", e.Encomienda?.Contenido ?? "", (e.Encomienda?.Monto ?? 0).ToString("N2", CultureInfo.InvariantCulture), e.Encomienda?.Estado == true ? "Activo" : "Anulado", e.Encomienda?.Pagado == true ? "Sí" : "No"];
    private static string[] ValuesWithoutConductor(Envio e) => [e.Fecha ?? "", e.HorarioId?.ToString() ?? "", e.Encomienda?.Numero ?? "", e.Encomienda?.FechaRecepcion ?? "", e.Encomienda?.FechaEntrega ?? "", e.Encomienda?.Destino ?? "", e.Encomienda?.Contenido ?? "", (e.Encomienda?.Monto ?? 0).ToString("N2", CultureInfo.InvariantCulture), e.Encomienda?.Estado == true ? "Activo" : "Anulado", e.Encomienda?.Pagado == true ? "Sí" : "No"];
    private static (string N, double W, int I)[] Cols() => [("Fecha envío", 62, 0), ("Horario", 40, 1), ("Nro. encomienda", 72, 2), ("Recepción", 66, 3), ("Entrega", 66, 4), ("Destino", 58, 5), ("Contenido", 115, 6), ("Monto", 48, 7), ("Estado", 45, 8), ("Pagado", 45, 9)];
    private static void ExcelHeader(IXLWorksheet s, string? usuario, List<string> filtros, Conductor? conductor = null) 
    { 
        s.Range("A1:F2").Merge(); 
        s.Cell("A1").Value = conductor != null 
            ? $"Reporte de encomiendas - {conductor.Nombres} {conductor.Apellidos}" 
            : "Reporte detallado por conductor"; 
        s.Cell("A1").Style.Font.Bold = true; 
        s.Cell("A1").Style.Font.FontSize = 16; 
        var p = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg"); 
        if (System.IO.File.Exists(p)) s.AddPicture(p).MoveTo(s.Cell("J1")).WithSize(110, 70); 
        s.Range("H3:J3").Merge(); 
        s.Range("H4:J4").Merge(); 
        s.Cell("H3").Value = $"Generado por: {usuario ?? "No especificado"}"; 
        s.Cell("H4").Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}"; 
        s.Range("H3:J4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; 
        s.Range("A5:J5").Merge(); 
        s.Cell("A5").Value = filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", filtros); 
        s.Cell("A5").Style.Alignment.WrapText = true; 
        if (conductor != null) 
        { 
            s.Range("A6:J6").Merge(); 
            s.Cell("A6").Value = $"Teléfono: {conductor.Telefono ?? "N/A"} | Licencia: {conductor.Licencia ?? "N/A"}"; 
            s.Cell("A6").Style.Font.Bold = true; 
        } 
    }
    private static void ExcelEnd(IXLWorksheet s, int row, int cols) { var r = s.Range(7, 1, 7, cols); r.Style.Font.Bold = true; r.Style.Fill.BackgroundColor = XLColor.LightGray; if (row > 8) s.Range(7, 1, row - 1, cols).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; s.Columns(1, cols).AdjustToContents(); s.SheetView.FreezeRows(7); }
    private sealed class Result { public Conductor? Conductor { get; set; } public List<Envio> Envios { get; set; } = []; public List<string> Filtros { get; set; } = []; public string? Error { get; set; } }
}

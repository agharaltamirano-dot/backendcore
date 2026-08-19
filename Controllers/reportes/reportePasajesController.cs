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
public class reportePasajesController : ControllerBase
{
    private readonly TransporteContext _context;
    public reportePasajesController(TransporteContext context) => _context = context;

    [HttpGet("reporte-pasajes/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportarPdf([FromQuery] int? clienteId, [FromQuery] int? asientoId, [FromQuery] string? destino, [FromQuery] bool? estado, [FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta, [FromQuery] string? movil, [FromQuery] int? usuarioId, [FromQuery] bool? reserva, [FromQuery] string? nombreUsuario)
    {
        var r = await Consultar(clienteId, asientoId, destino, estado, fechaDesde, fechaHasta, movil, usuarioId, reserva);
        if (r.Error != null) return BadRequest(r.Error);
        using var doc = new PdfDocument();
        XGraphics? g = null; double y = 0, w = 0, h = 0; const double m = 28;
        var title = new XFont("Arial", 15, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode)); var meta = new XFont("Arial", 8, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode)); var head = new XFont("Arial", 7, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode)); var body = new XFont("Arial", 6.5, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode)); var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo9.png");
        void Header() { var x = m; foreach (var c in Cols()) { g!.DrawRectangle(XBrushes.LightGray, x, y, c.W, 16); g.DrawString(c.N, head, XBrushes.Black, new XRect(x + 2, y + 3, c.W - 4, 10), XStringFormats.TopLeft); x += c.W; } y += 16; }
        void Page() { g?.Dispose(); var p = doc.AddPage(); p.Size = PdfSharpCore.PageSize.Letter; p.Orientation = PdfSharpCore.PageOrientation.Landscape; g = XGraphics.FromPdfPage(p); w = p.Width.Point; h = p.Height.Point; y = m; g.DrawString("Reporte detallado de pasajes", title, XBrushes.Black, new XRect(m, y, 400, 22), XStringFormats.TopLeft); if (System.IO.File.Exists(logoPath)) { using var logo = XImage.FromFile(logoPath); g.DrawImage(logo, w - m - 62, y, 62, 42); } y += 24; g.DrawString($"Generado por: {nombreUsuario ?? "No especificado"}", meta, XBrushes.Black, new XRect(w - m - 250, y, 250, 12), XStringFormats.TopRight); g.DrawString($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}", meta, XBrushes.Black, new XRect(w - m - 250, y + 11, 250, 12), XStringFormats.TopRight); y += 27; g.DrawString(r.Filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", r.Filtros), meta, XBrushes.Black, new XRect(m, y, w - m * 2, 28), XStringFormats.TopLeft); y += 30; Header(); }
        Page(); foreach (var p in r.Pasajes) { if (y + 22 > h - m) Page(); var x = m; var v = Values(p); foreach (var c in Cols()) { g!.DrawRectangle(XPens.LightGray, x, y, c.W, 22); g.DrawString(v[c.I], body, XBrushes.Black, new XRect(x + 2, y + 2, c.W - 4, 18), XStringFormats.TopLeft); x += c.W; } y += 22; } g?.Dispose(); using var ms = new MemoryStream(); doc.Save(ms, false); Response.Headers["Content-Disposition"] = "inline; filename=\"reporte_pasajes.pdf\""; return File(ms.ToArray(), "application/pdf");
    }

    [HttpGet("reporte-pasajes/xlsx")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportarXlsx([FromQuery] int? clienteId, [FromQuery] int? asientoId, [FromQuery] string? destino, [FromQuery] bool? estado, [FromQuery] string? fechaDesde, [FromQuery] string? fechaHasta, [FromQuery] string? movil, [FromQuery] int? usuarioId, [FromQuery] bool? reserva, [FromQuery] string? nombreUsuario)
    {
        var r = await Consultar(clienteId, asientoId, destino, estado, fechaDesde, fechaHasta, movil, usuarioId, reserva); if (r.Error != null) return BadRequest(r.Error);
        using var book = new XLWorkbook(); var s = book.Worksheets.Add("Pasajes"); ExcelHeader(s, "Reporte detallado de pasajes", nombreUsuario, r.Filtros, 11);
        var headers = new[] { "ID", "Fecha y hora", "Destino", "Móvil", "Monto (Bs)", "Estado", "Reserva", "Cliente", "Teléfono", "Nro. asiento", "Usuario" }; for (var i = 0; i < headers.Length; i++) s.Cell(7, i + 1).Value = headers[i]; var row = 8; foreach (var p in r.Pasajes) { var v = Values(p); for (var i = 0; i < v.Length; i++) s.Cell(row, i + 1).Value = v[i]; row++; } ExcelEnd(s, row, headers.Length); using var ms = new MemoryStream(); book.SaveAs(ms); return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reporte_pasajes.xlsx");
    }

    private async Task<Result> Consultar(int? clienteId, int? asientoId, string? destino, bool? estado, string? fechaDesde, string? fechaHasta, string? movil, int? usuarioId, bool? reserva)
    {
        if (new[] { fechaDesde, fechaHasta }.Any(x => x != null && !DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))) return new Result { Error = "Las fechas deben tener el formato yyyy-MM-dd." };
        var q = _context.Pasajes.AsNoTracking().Include(p => p.Cliente).Include(p => p.Asiento).Include(p => p.Usuario).AsQueryable(); if (clienteId.HasValue) q = q.Where(p => p.ClienteId == clienteId); if (asientoId.HasValue) q = q.Where(p => p.AsientoId == asientoId); if (!string.IsNullOrWhiteSpace(destino)) q = q.Where(p => p.Destino != null && EF.Functions.ILike(p.Destino, $"%{destino.Trim()}%")); if (estado.HasValue) q = q.Where(p => p.Estado == estado); if (reserva.HasValue) q = q.Where(p => p.Reserva == reserva); if (usuarioId.HasValue) q = q.Where(p => p.UsuarioId == usuarioId); if (!string.IsNullOrWhiteSpace(movil)) q = q.Where(p => p.Movil != null && EF.Functions.ILike(p.Movil, $"%{movil.Trim()}%")); if (fechaDesde != null) q = q.Where(p => p.FechaHora != null && string.Compare(p.FechaHora, fechaDesde + " 00:00") >= 0); if (fechaHasta != null) q = q.Where(p => p.FechaHora != null && string.Compare(p.FechaHora, fechaHasta + " 23:59:59") <= 0);
        var f = new List<string>(); if (clienteId.HasValue) f.Add($"Cliente ID: {clienteId}"); if (asientoId.HasValue) f.Add($"Asiento ID: {asientoId}"); if (!string.IsNullOrWhiteSpace(destino)) f.Add($"Destino: {destino.Trim()}"); if (estado.HasValue) f.Add($"Estado: {(estado.Value ? "Activo" : "Anulado")}"); if (fechaDesde != null) f.Add($"Fecha desde: {fechaDesde}"); if (fechaHasta != null) f.Add($"Fecha hasta: {fechaHasta}"); if (!string.IsNullOrWhiteSpace(movil)) f.Add($"Móvil: {movil.Trim()}"); if (usuarioId.HasValue) f.Add($"Usuario ID: {usuarioId}"); if (reserva.HasValue) f.Add($"Reserva: {(reserva.Value ? "Sí" : "No")}");
        return new Result { Pasajes = await q.OrderByDescending(p => p.Id).ToListAsync(), Filtros = f };
    }
    private static string[] Values(Pasaje p) => [p.Id.ToString(), p.FechaHora ?? "", p.Destino ?? "", p.Movil ?? "", p.Monto?.ToString("N2", CultureInfo.InvariantCulture) ?? "", p.Estado == true ? "Activo" : "Anulado", p.Reserva == true ? "Sí" : "No", p.Cliente?.NombreCompleto ?? "", p.Cliente?.Telefono ?? "", p.Asiento?.Numero?.ToString() ?? "", p.Usuario?.Usuario1 ?? ""];
    private static (string N, double W, int I)[] Cols() => [("ID", 30, 0), ("Fecha", 70, 1), ("Destino", 65, 2), ("Móvil", 48, 3), ("Monto", 48, 4), ("Estado", 45, 5), ("Reserva", 45, 6), ("Cliente", 90, 7), ("Teléfono", 68, 8), ("Asiento", 45, 9), ("Usuario", 55, 10)];
    private static void ExcelHeader(IXLWorksheet s, string title, string? user, List<string> filters, int logoCol) { s.Range("A1:F2").Merge(); s.Cell("A1").Value = title; s.Cell("A1").Style.Font.Bold = true; s.Cell("A1").Style.Font.FontSize = 16; var path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo9.png"); if (System.IO.File.Exists(path)) s.AddPicture(path).MoveTo(s.Cell(1, logoCol)).WithSize(110, 70); s.Range(3, logoCol - 2, 3, logoCol).Merge(); s.Range(4, logoCol - 2, 4, logoCol).Merge(); s.Cell(3, logoCol - 2).Value = $"Generado por: {user ?? "No especificado"}"; s.Cell(4, logoCol - 2).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}"; s.Range(3, logoCol - 2, 4, logoCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; s.Range(5, 1, 5, logoCol).Merge(); s.Cell(5, 1).Value = filters.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", filters); s.Cell(5, 1).Style.Alignment.WrapText = true; }
    private static void ExcelEnd(IXLWorksheet s, int row, int cols) { var r = s.Range(7, 1, 7, cols); r.Style.Font.Bold = true; r.Style.Fill.BackgroundColor = XLColor.LightGray; if (row > 8) s.Range(7, 1, row - 1, cols).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; s.Columns(1, cols).AdjustToContents(); s.SheetView.FreezeRows(7); }
    private sealed class Result { public List<Pasaje> Pasajes { get; set; } = []; public List<string> Filtros { get; set; } = []; public string? Error { get; set; } }
}

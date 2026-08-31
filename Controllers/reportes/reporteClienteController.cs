using backend.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace backend.Controllers.reportes;

[ApiController]
[Route("api/[controller]")]
public class reporteClienteController : ControllerBase
{
    private readonly TransporteContext _context;
    public reporteClienteController(TransporteContext context) => _context = context;

    [HttpGet("reporte-clientes/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportarPdf([FromQuery] bool? estado, [FromQuery] string? ci, [FromQuery] string? nombreCompleto, [FromQuery] string? telefono, [FromQuery] string? nombreUsuario)
    {
        var r = await Consultar(estado, ci, nombreCompleto, telefono);
        using var doc = new PdfDocument(); XGraphics? g = null; double y = 0, w = 0, h = 0; const double m = 36;
        var title = new XFont("Arial", 15, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode)); var meta = new XFont("Arial", 8, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode)); var head = new XFont("Arial", 9, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode)); var text = new XFont("Arial", 8, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode)); var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg");
        void TableHeader() { var x = m; foreach (var c in Cols()) { g!.DrawRectangle(XBrushes.LightGray, x, y, c.W, 18); g.DrawString(c.N, head, XBrushes.Black, new XRect(x + 3, y + 3, c.W - 6, 12), XStringFormats.TopLeft); x += c.W; } y += 18; }
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

    // Logo al lado izquierdo
    if (System.IO.File.Exists(logoPath)) 
    { 
        using var logo = XImage.FromFile(logoPath); 
        g.DrawImage(logo, m, y, 62, 42); 
    }

    // Título desplazado a la derecha del logo
    g.DrawString("Reporte detallado de clientes", title, XBrushes.Black, 
        new XRect(m + 70, y + 10, 400, 22), XStringFormats.TopLeft);

    y += 50; // bajar un poco para no chocar con el logo

    // Datos de usuario y fecha en la parte superior derecha
    g.DrawString($"Generado por: {nombreUsuario ?? "No especificado"}", meta, XBrushes.Black, 
        new XRect(w - m - 250, y, 250, 12), XStringFormats.TopRight); 
    g.DrawString($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}", meta, XBrushes.Black, 
        new XRect(w - m - 250, y + 11, 250, 12), XStringFormats.TopRight); 

    y += 27; 
    g.DrawString(r.Filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", r.Filtros), 
        meta, XBrushes.Black, new XRect(m, y, w - m * 2, 20), XStringFormats.TopLeft); 
    y += 22; 

    TableHeader(); 
}

        Page(); foreach (var c in r.Clientes) { if (y + 20 > h - m) Page(); var x = m; var v = Values(c); foreach (var col in Cols()) { g!.DrawRectangle(XPens.LightGray, x, y, col.W, 20); g.DrawString(v[col.I], text, XBrushes.Black, new XRect(x + 3, y + 3, col.W - 6, 14), XStringFormats.TopLeft); x += col.W; } y += 20; }
        g?.Dispose(); using var ms = new MemoryStream(); doc.Save(ms, false); Response.Headers["Content-Disposition"] = "inline; filename=\"reporte_clientes.pdf\""; return File(ms.ToArray(), "application/pdf");
    }

    [HttpGet("reporte-clientes/xlsx")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportarXlsx([FromQuery] bool? estado, [FromQuery] string? ci, [FromQuery] string? nombreCompleto, [FromQuery] string? telefono, [FromQuery] string? nombreUsuario)
    {
        var r = await Consultar(estado, ci, nombreCompleto, telefono); using var book = new XLWorkbook(); var s = book.Worksheets.Add("Clientes");
        s.Range("A1:C2").Merge(); s.Cell("A1").Value = "Reporte detallado de clientes"; s.Cell("A1").Style.Font.Bold = true; s.Cell("A1").Style.Font.FontSize = 16; var path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg"); if (System.IO.File.Exists(path)) s.AddPicture(path).MoveTo(s.Cell("E1")).WithSize(110, 70);
        s.Range("D3:F3").Merge(); s.Range("D4:F4").Merge(); s.Cell("D3").Value = $"Generado por: {nombreUsuario ?? "No especificado"}"; s.Cell("D4").Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}"; s.Range("D3:F4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; s.Range("A5:F5").Merge(); s.Cell("A5").Value = r.Filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", r.Filtros); s.Cell("A5").Style.Alignment.WrapText = true;
        var headers = new[] { "ID", "Nombre completo", "CI", "Teléfono", "Estado" }; for (var i = 0; i < headers.Length; i++) s.Cell(7, i + 1).Value = headers[i]; var row = 8; foreach (var c in r.Clientes) { var v = Values(c); for (var i = 0; i < v.Length; i++) s.Cell(row, i + 1).Value = v[i]; row++; }
        var range = s.Range(7, 1, 7, headers.Length); range.Style.Font.Bold = true; range.Style.Fill.BackgroundColor = XLColor.LightGray; if (row > 8) s.Range(7, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin; s.Columns(1, headers.Length).AdjustToContents(); s.SheetView.FreezeRows(7); using var ms = new MemoryStream(); book.SaveAs(ms); return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reporte_clientes.xlsx");
    }

    private async Task<Result> Consultar(bool? estado, string? ci, string? nombreCompleto, string? telefono)
    {
        var q = _context.Clientes.AsNoTracking().AsQueryable(); if (estado.HasValue) q = q.Where(c => c.Estado == estado); if (!string.IsNullOrWhiteSpace(ci)) q = q.Where(c => c.Ci != null && EF.Functions.ILike(c.Ci, $"%{ci.Trim()}%")); if (!string.IsNullOrWhiteSpace(nombreCompleto)) q = q.Where(c => c.NombreCompleto != null && EF.Functions.ILike(c.NombreCompleto, $"%{nombreCompleto.Trim()}%")); if (!string.IsNullOrWhiteSpace(telefono)) q = q.Where(c => c.Telefono != null && EF.Functions.ILike(c.Telefono, $"%{telefono.Trim()}%"));
        var f = new List<string>(); if (estado.HasValue) f.Add($"Estado: {(estado.Value ? "Activo" : "Inactivo")}"); if (!string.IsNullOrWhiteSpace(ci)) f.Add($"CI: {ci.Trim()}"); if (!string.IsNullOrWhiteSpace(nombreCompleto)) f.Add($"Nombre completo: {nombreCompleto.Trim()}"); if (!string.IsNullOrWhiteSpace(telefono)) f.Add($"Teléfono: {telefono.Trim()}"); return new Result { Clientes = await q.OrderBy(c => c.NombreCompleto).ToListAsync(), Filtros = f };
    }
    private static string[] Values(Cliente c) => [c.Id.ToString(), c.NombreCompleto ?? "", c.Ci ?? "", c.Telefono ?? "", c.Estado == true ? "Activo" : "Inactivo"];
    private static (string N, double W, int I)[] Cols() => [("ID", 60, 0), ("Nombre completo", 260, 1), ("CI", 150, 2), ("Teléfono", 150, 3), ("Estado", 120, 4)];
    private sealed class Result { public List<Cliente> Clientes { get; set; } = []; public List<string> Filtros { get; set; } = []; }
}

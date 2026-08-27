using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace backend.Controllers.reportes
{
   [ApiController]
[Route("api/reporteIngresos")]
public class ReportesController : ControllerBase
{
    private readonly TransporteContext _context;

    public ReportesController(TransporteContext context)
    {
        _context = context;
    }
private async Task<(List<Pasaje> pasajes, List<Encomiendum> encomiendas)> BuscarDatos(
    int? usuarioId, string? fechaInicio, string? fechaFin, bool? estado)
{
    var pasajesQuery = _context.Pasajes.AsQueryable();
    var encomiendasQuery = _context.Encomienda.AsQueryable();

    if (usuarioId.HasValue)
    {
        pasajesQuery = pasajesQuery.Where(p => p.UsuarioId == usuarioId);
        encomiendasQuery = encomiendasQuery.Where(e => e.UsuarioId == usuarioId);
    }

    if (!string.IsNullOrEmpty(fechaInicio))
    {
        pasajesQuery = pasajesQuery.Where(p => p.FechaHora != null && string.Compare(p.FechaHora, fechaInicio) >= 0);
        encomiendasQuery = encomiendasQuery.Where(e => e.FechaRecepcion != null && string.Compare(e.FechaRecepcion, fechaInicio) >= 0);
    }

    if (!string.IsNullOrEmpty(fechaFin))
    {
        pasajesQuery = pasajesQuery.Where(p => p.FechaHora != null && string.Compare(p.FechaHora, fechaFin + " 23:59:59") <= 0);
        encomiendasQuery = encomiendasQuery.Where(e => e.FechaRecepcion != null && string.Compare(e.FechaRecepcion, fechaFin + " 23:59:59") <= 0);
    }

    if (estado.HasValue)
    {
        pasajesQuery = pasajesQuery.Where(p => p.Estado == estado);
        encomiendasQuery = encomiendasQuery.Where(e => e.Estado == estado);
    }

    var pasajes = await pasajesQuery.Include(p => p.Usuario).ToListAsync();
    var encomiendas = await encomiendasQuery.Include(e => e.Usuario).ToListAsync();

    return (pasajes, encomiendas);
}

[HttpGet("resumen/json")]
[AllowAnonymous]
public async Task<IActionResult> GetResumenJson(
    [FromQuery] int? usuarioId,
    [FromQuery] string? fechaInicio,
    [FromQuery] string? fechaFin,
    [FromQuery] bool? estado)
{
    DateTime? fi = string.IsNullOrEmpty(fechaInicio) ? null : DateTime.ParseExact(fechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    DateTime? ff = string.IsNullOrEmpty(fechaFin) ? null : DateTime.ParseExact(fechaFin, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    var (pasajes, encomiendas) = await BuscarDatos(usuarioId, fechaInicio, fechaFin, estado);

    var resumenPasajes = pasajes.GroupBy(p => DateTime.Parse(p.FechaHora!).Date)
        .Select(g => new {
            Fecha = g.Key.ToString("yyyy-MM-dd"),
            CantPasajes = g.Count(),
            SumaPasajes = g.Sum(p => p.Monto ?? 0),
            ActivosPasajes = g.Where(p => p.Estado == true).Sum(p => p.Monto ?? 0),
            AnuladosPasajes = g.Where(p => p.Estado == false).Sum(p => p.Monto ?? 0)
        }).ToList();

    var resumenEnc = encomiendas.GroupBy(e => DateTime.Parse(e.FechaRecepcion!).Date)
        .Select(g => new {
            Fecha = g.Key.ToString("yyyy-MM-dd"),
            CantEncomiendas = g.Count(),
            SumaEncomiendas = g.Sum(e => e.Monto ?? 0),
            ActivosEnc = g.Where(e => e.Estado == true).Sum(e => e.Monto ?? 0),
            AnuladosEnc = g.Where(e => e.Estado == false).Sum(e => e.Monto ?? 0)
        }).ToList();

    // Unir por fecha
    var fechas = resumenPasajes.Select(r => r.Fecha).Union(resumenEnc.Select(r => r.Fecha)).OrderBy(f => f);

    var resultado = fechas.Select(fecha => new {
        Fecha = fecha,
        CantPasajes = resumenPasajes.FirstOrDefault(r => r.Fecha == fecha)?.CantPasajes ?? 0,
        CantEncomiendas = resumenEnc.FirstOrDefault(r => r.Fecha == fecha)?.CantEncomiendas ?? 0,
        Total = (resumenPasajes.FirstOrDefault(r => r.Fecha == fecha)?.SumaPasajes ?? 0)
              + (resumenEnc.FirstOrDefault(r => r.Fecha == fecha)?.SumaEncomiendas ?? 0),
        Activos = (resumenPasajes.FirstOrDefault(r => r.Fecha == fecha)?.ActivosPasajes ?? 0)
                + (resumenEnc.FirstOrDefault(r => r.Fecha == fecha)?.ActivosEnc ?? 0),
        Anulados = (resumenPasajes.FirstOrDefault(r => r.Fecha == fecha)?.AnuladosPasajes ?? 0)
                 + (resumenEnc.FirstOrDefault(r => r.Fecha == fecha)?.AnuladosEnc ?? 0)
    }).ToList();

    return Ok(new { resumen = resultado });
}

    // GET: api/reporteIngresos/resumen/pdf
    [HttpGet("resumen/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> PdfResumen([FromQuery] int? usuarioId, [FromQuery] string? fechaInicio, [FromQuery] string? fechaFin, [FromQuery] bool? estado, [FromQuery] string? nombreUsuario)
    {
        var (pasajes, encomiendas) = await BuscarDatos(usuarioId, fechaInicio, fechaFin, estado);

        using var doc = new PdfDocument();
        var page = doc.AddPage();
        page.Size = PdfSharpCore.PageSize.Letter;
        page.Orientation = PdfSharpCore.PageOrientation.Landscape;
        var g = XGraphics.FromPdfPage(page);

        var title = new XFont("Arial", 15, XFontStyle.Bold);
        var head = new XFont("Arial", 9, XFontStyle.Bold);
        var body = new XFont("Arial", 8, XFontStyle.Regular);

        g.DrawString("Reporte resumido de ingresos", title, XBrushes.Black, new XRect(40, 40, page.Width, 30), XStringFormats.TopLeft);
        g.DrawString($"Generado por: {nombreUsuario ?? "No especificado"}", body, XBrushes.Black, new XRect(page.Width - 250, 40, 200, 20), XStringFormats.TopRight);
        g.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}", body, XBrushes.Black, new XRect(page.Width - 250, 60, 200, 20), XStringFormats.TopRight);

        int y = 100;
        g.DrawString("Fecha", head, XBrushes.Black, new XRect(40, y, 80, 20), XStringFormats.TopLeft);
        g.DrawString("Cant. Pasajes", head, XBrushes.Black, new XRect(130, y, 100, 20), XStringFormats.TopLeft);
        g.DrawString("Cant. Encomiendas", head, XBrushes.Black, new XRect(240, y, 120, 20), XStringFormats.TopLeft);
        g.DrawString("Total Bs", head, XBrushes.Black, new XRect(370, y, 80, 20), XStringFormats.TopLeft);
        g.DrawString("Activos Bs", head, XBrushes.Green, new XRect(460, y, 80, 20), XStringFormats.TopLeft);
        g.DrawString("Anulados Bs", head, XBrushes.Red, new XRect(550, y, 80, 20), XStringFormats.TopLeft);

        y += 30;

        var resumen = pasajes.GroupBy(p => DateTime.Parse(p.FechaHora).Date)
            .Select(gp => new {
                Fecha = gp.Key,
                CantPasajes = gp.Count(),
                SumaPasajes = gp.Sum(p => p.Monto ?? 0),
                ActivosPasajes = gp.Where(p => p.Estado == true).Sum(p => p.Monto ?? 0),
                AnuladosPasajes = gp.Where(p => p.Estado == false).Sum(p => p.Monto ?? 0)
            }).ToList();

        var resumenEnc = encomiendas.GroupBy(e => DateTime.Parse(e.FechaRecepcion).Date)
            .Select(ge => new {
                Fecha = ge.Key,
                CantEncomiendas = ge.Count(),
                SumaEncomiendas = ge.Sum(e => e.Monto ?? 0),
                ActivosEnc = ge.Where(e => e.Estado == true).Sum(e => e.Monto ?? 0),
                AnuladosEnc = ge.Where(e => e.Estado == false).Sum(e => e.Monto ?? 0)
            }).ToList();

        var fechas = resumen.Select(r => r.Fecha).Union(resumenEnc.Select(r => r.Fecha)).OrderBy(f => f);

        foreach (var fecha in fechas)
        {
            var rp = resumen.FirstOrDefault(r => r.Fecha == fecha);
            var re = resumenEnc.FirstOrDefault(r => r.Fecha == fecha);

            int cantPasajes = rp?.CantPasajes ?? 0;
            int cantEnc = re?.CantEncomiendas ?? 0;
            double total = (rp?.SumaPasajes ?? 0) + (re?.SumaEncomiendas ?? 0);
            double activos = (rp?.ActivosPasajes ?? 0) + (re?.ActivosEnc ?? 0);
            double anulados = (rp?.AnuladosPasajes ?? 0) + (re?.AnuladosEnc ?? 0);

            g.DrawString(fecha.ToString("yyyy-MM-dd"), body, XBrushes.Black, new XRect(40, y, 80, 20), XStringFormats.TopLeft);
            g.DrawString(cantPasajes.ToString(), body, XBrushes.Black, new XRect(130, y, 100, 20), XStringFormats.TopLeft);
            g.DrawString(cantEnc.ToString(), body, XBrushes.Black, new XRect(240, y, 120, 20), XStringFormats.TopLeft);
            g.DrawString(total.ToString("N2", CultureInfo.InvariantCulture), body, XBrushes.Black, new XRect(370, y, 80, 20), XStringFormats.TopLeft);
            g.DrawString(activos.ToString("N2", CultureInfo.InvariantCulture), body, XBrushes.Green, new XRect(460, y, 80, 20), XStringFormats.TopLeft);
            g.DrawString(anulados.ToString("N2", CultureInfo.InvariantCulture), body, XBrushes.Red, new XRect(550, y, 80, 20), XStringFormats.TopLeft);

            y += 20;
        }

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        Response.Headers["Content-Disposition"] = "inline; filename=\"reporte_resumen.pdf\"";
        return File(ms.ToArray(), "application/pdf");
    }

    [HttpGet("detallado/pdf")]
[AllowAnonymous]
public async Task<IActionResult> PdfDetallado(
    [FromQuery] int? usuarioId,
    [FromQuery] string? fechaInicio,
    [FromQuery] string? fechaFin,
    [FromQuery] bool? estado,
    [FromQuery] string? nombreUsuario)
{
    // Parse fechas
    DateTime? fi = string.IsNullOrEmpty(fechaInicio) ? null : DateTime.ParseExact(fechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    DateTime? ff = string.IsNullOrEmpty(fechaFin) ? null : DateTime.ParseExact(fechaFin, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    var (pasajes, encomiendas) = await BuscarDatos(usuarioId, fechaInicio, fechaFin, estado);

    using var doc = new PdfDocument();
    var page = doc.AddPage();
    page.Size = PdfSharpCore.PageSize.Letter;
    page.Orientation = PdfSharpCore.PageOrientation.Landscape;
    var g = XGraphics.FromPdfPage(page);

    var title = new XFont("Arial", 15, XFontStyle.Bold);
    var head = new XFont("Arial", 9, XFontStyle.Bold);
    var body = new XFont("Arial", 8, XFontStyle.Regular);

    // Encabezado
    g.DrawString("Reporte detallado de ingresos", title, XBrushes.Black, new XRect(40, 40, page.Width, 30), XStringFormats.TopLeft);
    g.DrawString($"Generado por: {nombreUsuario ?? "No especificado"}", body, XBrushes.Black, new XRect(page.Width - 250, 40, 200, 20), XStringFormats.TopRight);
    g.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}", body, XBrushes.Black, new XRect(page.Width - 250, 60, 200, 20), XStringFormats.TopRight);

    int y = 100;
    g.DrawString("Fecha/Hora", head, XBrushes.Black, new XRect(40, y, 120, 20), XStringFormats.TopLeft);
    g.DrawString("Tipo", head, XBrushes.Black, new XRect(170, y, 60, 20), XStringFormats.TopLeft);
    g.DrawString("Usuario", head, XBrushes.Black, new XRect(240, y, 120, 20), XStringFormats.TopLeft);
    g.DrawString("Estado", head, XBrushes.Black, new XRect(370, y, 80, 20), XStringFormats.TopLeft);
    g.DrawString("Monto Bs", head, XBrushes.Black, new XRect(460, y, 80, 20), XStringFormats.TopLeft);

    y += 30;

    // Unir pasajes y encomiendas en un solo listado
    var registros = pasajes.Select(p => new RegistroReporte
{
    Fecha = DateTime.Parse(p.FechaHora!),
    Tipo = "Pasaje",
    Usuario = p.Usuario?.Usuario1 ?? "",
    Estado = p.Estado,
    Monto = (double)(p.Monto ?? 0)
})
.Concat(encomiendas.Select(e => new RegistroReporte
{
    Fecha = DateTime.Parse(e.FechaRecepcion!),
    Tipo = "Encomienda",
    Usuario = e.Usuario?.Usuario1 ?? "",
    Estado = e.Estado,
    Monto = e.Monto ?? 0
}))
.OrderBy(r => r.Fecha)
.ToList();


    // Agrupar por día
    var grupos = registros.GroupBy(r => r.Fecha.Date);

    foreach (var grupo in grupos)
    {
        // Cabecera del día
        g.DrawString($"Día: {grupo.Key:yyyy-MM-dd}", head, XBrushes.DarkBlue, new XRect(40, y, 200, 20), XStringFormats.TopLeft);
        y += 25;

        double totalDia = 0, activosDia = 0, anuladosDia = 0;

        foreach (var r in grupo)
        {
            if (y + 20 > page.Height - 40)
            {
                page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.Letter;
                page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                g = XGraphics.FromPdfPage(page);
                y = 100;
            }

            g.DrawString(r.Fecha.ToString("yyyy-MM-dd HH:mm:ss"), body, XBrushes.Black, new XRect(40, y, 120, 20), XStringFormats.TopLeft);
            g.DrawString(r.Tipo, body, XBrushes.Black, new XRect(170, y, 60, 20), XStringFormats.TopLeft);
            g.DrawString(r.Usuario, body, XBrushes.Black, new XRect(240, y, 120, 20), XStringFormats.TopLeft);

            var brush = r.Estado == true ? XBrushes.Green : XBrushes.Red;
            g.DrawString(r.Estado == true ? "Activo" : "Anulado", body, brush, new XRect(370, y, 80, 20), XStringFormats.TopLeft);

            g.DrawString(r.Monto.ToString("N2", CultureInfo.InvariantCulture), body, XBrushes.Black, new XRect(460, y, 80, 20), XStringFormats.TopLeft);

            totalDia += r.Monto;
            if (r.Estado == true) activosDia += r.Monto;
            else anuladosDia += r.Monto;

            y += 20;
        }

        // Resumen del día
        g.DrawString($"Resumen {grupo.Key:yyyy-MM-dd} → Total: {totalDia:N2} Bs | Activos: {activosDia:N2} Bs | Anulados: {anuladosDia:N2} Bs",
            head, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
        y += 30;
    }

    using var ms = new MemoryStream();
    doc.Save(ms, false);
    Response.Headers["Content-Disposition"] = "inline; filename=\"reporte_detallado.pdf\"";
    return File(ms.ToArray(), "application/pdf");
}


    [HttpGet("resumen/xlsx")]
[AllowAnonymous]
public async Task<IActionResult> XlsxResumen(
    [FromQuery] int? usuarioId,
    [FromQuery] string? fechaInicio,
    [FromQuery] string? fechaFin,
    [FromQuery] bool? estado,
    [FromQuery] string? nombreUsuario)
{
    // Parse fechas
    DateTime? fi = string.IsNullOrEmpty(fechaInicio) ? null : DateTime.ParseExact(fechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    DateTime? ff = string.IsNullOrEmpty(fechaFin) ? null : DateTime.ParseExact(fechaFin, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    var (pasajes, encomiendas) = await BuscarDatos(usuarioId, fechaInicio, fechaFin, estado);

    using var book = new XLWorkbook();
    var s = book.Worksheets.Add("Resumen");

    // Encabezado
    s.Range("A1:F2").Merge();
    s.Cell("A1").Value = "Reporte resumido de ingresos";
    s.Cell("A1").Style.Font.Bold = true;
    s.Cell("A1").Style.Font.FontSize = 16;

    var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo9.png");
    if (System.IO.File.Exists(logoPath))
        s.AddPicture(logoPath).MoveTo(s.Cell("H1")).WithSize(110, 70);

    s.Range("F3:H3").Merge();
    s.Range("F4:H4").Merge();
    s.Cell("F3").Value = $"Generado por: {nombreUsuario ?? "No especificado"}";
    s.Cell("F4").Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
    s.Range("F3:H4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

    // Filtros aplicados
    s.Range("A5:H5").Merge();
    var filtros = new List<string>();
    if (fi.HasValue) filtros.Add($"Fecha inicio: {fi:yyyy-MM-dd}");
    if (ff.HasValue) filtros.Add($"Fecha fin: {ff:yyyy-MM-dd}");
    if (usuarioId.HasValue) filtros.Add($"Usuario ID: {usuarioId}");
    if (estado.HasValue) filtros.Add($"Estado: {(estado.Value ? "Activo" : "Anulado")}");
    s.Cell("A5").Value = filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", filtros);
    s.Cell("A5").Style.Alignment.WrapText = true;

    // Cabeceras
    var headers = new[] { "Fecha", "Cant. Pasajes", "Cant. Encomiendas", "Total Bs", "Activos Bs", "Anulados Bs" };
    for (var i = 0; i < headers.Length; i++)
        s.Cell(7, i + 1).Value = headers[i];

    var row = 8;

    // Resumen por día
    var resumenPasajes = pasajes.GroupBy(p => DateTime.Parse(p.FechaHora!).Date)
        .Select(g => new {
            Fecha = g.Key,
            CantPasajes = g.Count(),
            SumaPasajes = g.Sum(p => p.Monto ?? 0),
            ActivosPasajes = g.Where(p => p.Estado == true).Sum(p => p.Monto ?? 0),
            AnuladosPasajes = g.Where(p => p.Estado == false).Sum(p => p.Monto ?? 0)
        }).ToList();

    var resumenEnc = encomiendas.GroupBy(e => DateTime.Parse(e.FechaRecepcion!).Date)
        .Select(g => new {
            Fecha = g.Key,
            CantEncomiendas = g.Count(),
            SumaEncomiendas = g.Sum(e => e.Monto ?? 0),
            ActivosEnc = g.Where(e => e.Estado == true).Sum(e => e.Monto ?? 0),
            AnuladosEnc = g.Where(e => e.Estado == false).Sum(e => e.Monto ?? 0)
        }).ToList();

    var fechas = resumenPasajes.Select(r => r.Fecha).Union(resumenEnc.Select(r => r.Fecha)).OrderBy(f => f);

    foreach (var fecha in fechas)
    {
        var rp = resumenPasajes.FirstOrDefault(r => r.Fecha == fecha);
        var re = resumenEnc.FirstOrDefault(r => r.Fecha == fecha);

        int cantPasajes = rp?.CantPasajes ?? 0;
        int cantEnc = re?.CantEncomiendas ?? 0;
        double total = (rp?.SumaPasajes ?? 0) + (re?.SumaEncomiendas ?? 0);
        double activos = (rp?.ActivosPasajes ?? 0) + (re?.ActivosEnc ?? 0);
        double anulados = (rp?.AnuladosPasajes ?? 0) + (re?.AnuladosEnc ?? 0);

        s.Cell(row, 1).Value = fecha.ToString("yyyy-MM-dd");
        s.Cell(row, 2).Value = cantPasajes;
        s.Cell(row, 3).Value = cantEnc;
        s.Cell(row, 4).Value = total;
        s.Cell(row, 5).Value = activos;
        s.Cell(row, 6).Value = anulados;

        // Colores UX
        s.Cell(row, 5).Style.Font.FontColor = XLColor.Green;
        s.Cell(row, 6).Style.Font.FontColor = XLColor.Red;

        row++;
    }

    // Estilo final
    var r = s.Range(7, 1, 7, headers.Length);
    r.Style.Font.Bold = true;
    r.Style.Fill.BackgroundColor = XLColor.LightGray;
    if (row > 8) s.Range(7, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    s.Columns(1, headers.Length).AdjustToContents();
    s.SheetView.FreezeRows(7);

    using var ms = new MemoryStream();
    book.SaveAs(ms);
    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reporte_resumen.xlsx");
}
[HttpGet("detallado/xlsx")]
[AllowAnonymous]
public async Task<IActionResult> XlsxDetallado(
    [FromQuery] int? usuarioId,
    [FromQuery] string? fechaInicio,
    [FromQuery] string? fechaFin,
    [FromQuery] bool? estado,
    [FromQuery] string? nombreUsuario)
{
    DateTime? fi = string.IsNullOrEmpty(fechaInicio) ? null : DateTime.ParseExact(fechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    DateTime? ff = string.IsNullOrEmpty(fechaFin) ? null : DateTime.ParseExact(fechaFin, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    var (pasajes, encomiendas) = await BuscarDatos(usuarioId, fechaInicio, fechaFin, estado);

    using var book = new XLWorkbook();
    var s = book.Worksheets.Add("Detallado");

    // Encabezado
    s.Range("A1:F2").Merge();
    s.Cell("A1").Value = "Reporte detallado de ingresos";
    s.Cell("A1").Style.Font.Bold = true;
    s.Cell("A1").Style.Font.FontSize = 16;

    var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo9.png");
    if (System.IO.File.Exists(logoPath))
        s.AddPicture(logoPath).MoveTo(s.Cell("H1")).WithSize(110, 70);

    s.Range("F3:H3").Merge();
    s.Range("F4:H4").Merge();
    s.Cell("F3").Value = $"Generado por: {nombreUsuario ?? "No especificado"}";
    s.Cell("F4").Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
    s.Range("F3:H4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

    // Filtros aplicados
    s.Range("A5:H5").Merge();
    var filtros = new List<string>();
    if (fi.HasValue) filtros.Add($"Fecha inicio: {fi:yyyy-MM-dd}");
    if (ff.HasValue) filtros.Add($"Fecha fin: {ff:yyyy-MM-dd}");
    if (usuarioId.HasValue) filtros.Add($"Usuario ID: {usuarioId}");
    if (estado.HasValue) filtros.Add($"Estado: {(estado.Value ? "Activo" : "Anulado")}");
    s.Cell("A5").Value = filtros.Count == 0 ? "Criterios: sin filtros" : "Criterios: " + string.Join(" | ", filtros);
    s.Cell("A5").Style.Alignment.WrapText = true;

    // Cabeceras
    var headers = new[] { "Fecha/Hora", "Tipo", "Usuario", "Estado", "Monto Bs" };
    for (var i = 0; i < headers.Length; i++)
        s.Cell(7, i + 1).Value = headers[i];

    var row = 8;

    // Unir pasajes y encomiendas en un solo listado
    var registros = pasajes.Select(p => new RegistroReporte
    {
        Fecha = DateTime.Parse(p.FechaHora!),
        Tipo = "Pasaje",
        Usuario = p.Usuario?.Usuario1 ?? "",
        Estado = p.Estado,
        Monto = (double)(p.Monto ?? 0)
    })
    .Concat(encomiendas.Select(e => new RegistroReporte
    {
        Fecha = DateTime.Parse(e.FechaRecepcion!),
        Tipo = "Encomienda",
        Usuario = e.Usuario?.Usuario1 ?? "",
        Estado = e.Estado,
        Monto = e.Monto ?? 0
    }))
    .OrderBy(r => r.Fecha)
    .ToList();

    // Agrupar por día
    var grupos = registros.GroupBy(r => r.Fecha.Date);

    foreach (var grupo in grupos)
    {
        // Cabecera del día
        s.Cell(row, 1).Value = $"Día: {grupo.Key:yyyy-MM-dd}";
        s.Range(row, 1, row, headers.Length).Merge();
        s.Cell(row, 1).Style.Font.Bold = true;
        s.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        row++;

        double totalDia = 0, activosDia = 0, anuladosDia = 0;

        foreach (var r in grupo)
        {
            s.Cell(row, 1).Value = r.Fecha.ToString("yyyy-MM-dd HH:mm:ss");
            s.Cell(row, 2).Value = r.Tipo;
            s.Cell(row, 3).Value = r.Usuario;
            s.Cell(row, 4).Value = r.Estado == true ? "Activo" : "Anulado";
            s.Cell(row, 5).Value = r.Monto;

            if (r.Estado == true)
            {
                s.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                activosDia += r.Monto;
            }
            else
            {
                s.Cell(row, 4).Style.Font.FontColor = XLColor.Red;
                anuladosDia += r.Monto;
            }

            totalDia += r.Monto;
            row++;
        }

        // Resumen del día
        s.Cell(row, 1).Value = $"Resumen {grupo.Key:yyyy-MM-dd}";
        s.Cell(row, 2).Value = $"Total: {totalDia:N2} Bs";
        s.Cell(row, 3).Value = $"Activos: {activosDia:N2} Bs";
        s.Cell(row, 4).Value = $"Anulados: {anuladosDia:N2} Bs";
        s.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        s.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGray;
        row += 2;
    }

    // Ajustes finales
    s.Columns(1, headers.Length).AdjustToContents();
    s.SheetView.FreezeRows(7);

    using var ms = new MemoryStream();
    book.SaveAs(ms);
    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reporte_detallado.xlsx");
}

}
public class RegistroReporte
{
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = "";
    public string Usuario { get; set; } = "";
    public bool? Estado { get; set; }
    public double Monto { get; set; }
}


}

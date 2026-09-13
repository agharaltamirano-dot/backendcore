using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace backend.Controllers.reportes
{
    [Route("api/[controller]")]
    public class reporteEncomiendas : Controller
    {
        private readonly TransporteContext _context;


        public reporteEncomiendas(TransporteContext context)
        {
            _context = context;
        }

        [HttpGet("reporte-encomiendas/json")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReporteEncomiendasJson(
            [FromQuery] int? clienteRemitenteId,
            [FromQuery] int? clienteConsignatarioId,
            [FromQuery] string? destino,
            [FromQuery] bool? estado,
            [FromQuery] string? recepcionFechaDesde,
            [FromQuery] string? recepcionFechaHasta,
            [FromQuery] string? entregaFechaDesde,
            [FromQuery] string? entregaFechaHasta,
            [FromQuery] string? numero,
            [FromQuery] bool? pagado,
            [FromQuery] int? usuarioId,
            [FromQuery] string? nombreUsuario)
        {
            try
            {
                var result = await ObtenerReporteEncomiendas(
                    clienteRemitenteId, clienteConsignatarioId, destino, estado,
                    recepcionFechaDesde, recepcionFechaHasta, entregaFechaDesde, entregaFechaHasta,
                    numero, pagado, usuarioId, nombreUsuario);

                if (result.Error != null)
                {
                    Console.WriteLine($"[ERROR] Filtros inválidos o fallo en consulta: {result.Error}");
                    return BadRequest(result.Error);
                }

                // Devolver la lista filtrada y los filtros en un objeto simple
                // para que el front reciba exactamente los datos que espera.
                return Ok(new { encomiendas = result.Encomiendas, filtros = result.Filtros });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Error en reporte-encomiendas/json: {ex}");
                return StatusCode(500, new { error = "Error generando reporte de encomiendas", detalle = ex.Message });
            }
        }

      [HttpGet("reporte-encomiendas/pdf")]
[AllowAnonymous]
public async Task<IActionResult> ExportarReporteEncomiendasPdf(
    [FromQuery] int? clienteRemitenteId,
    [FromQuery] int? clienteConsignatarioId,
    [FromQuery] string? destino,
    [FromQuery] bool? estado,
    [FromQuery] string? recepcionFechaDesde,
    [FromQuery] string? recepcionFechaHasta,
    [FromQuery] string? entregaFechaDesde,
    [FromQuery] string? entregaFechaHasta,
    [FromQuery] string? numero,
    [FromQuery] bool? pagado,
    [FromQuery] int? usuarioId,
    [FromQuery] string? nombreUsuario)
{
    try
    {
        var resultado = await ObtenerReporteEncomiendas(
            clienteRemitenteId, clienteConsignatarioId, destino, estado,
            recepcionFechaDesde, recepcionFechaHasta, entregaFechaDesde, entregaFechaHasta,
            numero, pagado, usuarioId, nombreUsuario);

        if (resultado.Error != null)
            return BadRequest(resultado.Error);

        using var documento = new PdfDocument();
        const double margen = 28;
        var fechaGeneracion = DateTime.Now;
        XGraphics? grafico = null;
        double y = 0, anchoPagina = 0, altoPagina = 0;

        var tituloFont = new XFont("Arial", 15, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
        var metaFont = new XFont("Arial", 8, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        var filtroFont = new XFont("Arial", 7, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        var encabezadoFont = new XFont("Arial", 7, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
        var contenidoFont = new XFont("Arial", 6.5, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg");

        void CrearPagina()
        {
            grafico?.Dispose();
            var pagina = documento.AddPage();
            pagina.Size = PdfSharpCore.PageSize.Letter;
            pagina.Orientation = PdfSharpCore.PageOrientation.Landscape;
            grafico = XGraphics.FromPdfPage(pagina);
            anchoPagina = pagina.Width.Point;
            altoPagina = pagina.Height.Point;
            y = margen;

            // Logo a la izquierda para evitar solapamiento con metadatos
            if (System.IO.File.Exists(logoPath))
            {
                using var logo = XImage.FromFile(logoPath);
                grafico.DrawImage(logo, margen, y, 62, 42);
            }

            // Título desplazado a la derecha del logo
            grafico.DrawString("Reporte detallado de encomiendas", tituloFont, XBrushes.Black,
                new XRect(margen + 80, y, anchoPagina - (margen * 2) - 80, 22), XStringFormats.TopLeft);

            y += 24;
            grafico.DrawString($"Generado por: {nombreUsuario ?? "No especificado"}", metaFont, XBrushes.Black,
                new XRect(anchoPagina - margen - 250, y, 250, 14), XStringFormats.TopRight);
            grafico.DrawString($"Fecha de generación: {fechaGeneracion:dd/MM/yyyy HH:mm}", metaFont, XBrushes.Black,
                new XRect(anchoPagina - margen - 250, y + 11, 250, 14), XStringFormats.TopRight);
            y += 27;

            var filtros = resultado.Filtros.Count == 0
                ? "Criterios: sin filtros"
                : "Criterios: " + string.Join(" | ", resultado.Filtros);
            grafico.DrawString(filtros, filtroFont, XBrushes.Black,
                new XRect(margen, y, anchoPagina - (margen * 2), 28), XStringFormats.TopLeft);
            y += 30;

            DibujarEncabezadoTabla();
        }

        void DibujarEncabezadoTabla()
        {
            var columnas = ColumnasPdf();
            var x = margen;
            foreach (var columna in columnas)
            {
                grafico!.DrawRectangle(XBrushes.LightGray, x, y, columna.Ancho, 16);
                grafico.DrawString(columna.Nombre, encabezadoFont, XBrushes.Black,
                    new XRect(x + 2, y + 3, columna.Ancho - 4, 10), XStringFormats.TopLeft);
                x += columna.Ancho;
            }
            y += 16;
        }

        CrearPagina();
        foreach (var encomienda in resultado.Encomiendas)
        {
            const double altoFila = 24;
            if (y + altoFila > altoPagina - margen)
                CrearPagina();

            var valores = ValoresEncomienda(encomienda);
            var x = margen;
            foreach (var columna in ColumnasPdf())
            {
                grafico!.DrawRectangle(XPens.LightGray, x, y, columna.Ancho, altoFila);
                grafico.DrawString(valores[columna.Indice], contenidoFont, XBrushes.Black,
                    new XRect(x + 2, y + 2, columna.Ancho - 4, altoFila - 4), XStringFormats.TopLeft);
                x += columna.Ancho;
            }
            y += altoFila;
        }

        // --- Resumen al final del PDF ---
        try
        {
            var totalCount = resultado.Encomiendas.Count;
            var activos = resultado.Encomiendas.Count(e => e.Estado == true);
            var anulados = totalCount - activos;
            var pagadas = resultado.Encomiendas.Count(e => e.Pagado == true);
            var porPagar = totalCount - pagadas;
            var montoTotal = resultado.Encomiendas.Where(e => e.Monto.HasValue).Sum(e => e.Monto ?? 0);
            var montoActivos = resultado.Encomiendas.Where(e => e.Estado == true && e.Monto.HasValue).Sum(e => e.Monto ?? 0);
            var montoAnulados = resultado.Encomiendas.Where(e => (e.Estado != true) && e.Monto.HasValue).Sum(e => e.Monto ?? 0);
            var montoPagadas = resultado.Encomiendas.Where(e => e.Pagado == true && e.Monto.HasValue).Sum(e => e.Monto ?? 0);
            var montoPorPagar = resultado.Encomiendas.Where(e => (e.Pagado != true) && e.Monto.HasValue).Sum(e => e.Monto ?? 0);

            const double summaryHeight = 54;
            if (y + summaryHeight > altoPagina - margen)
                CrearPagina();

            var summaryX = margen;
            var summaryWidth = anchoPagina - (margen * 2);
            grafico!.DrawRectangle(XPens.LightGray, summaryX, y, summaryWidth, summaryHeight);
            var left = summaryX + 6;
            var top = y + 6;

            // Conteos
            grafico.DrawString($"Total: {totalCount}", encabezadoFont, XBrushes.Black, new XRect(left, top, 140, 12), XStringFormats.TopLeft);
            grafico.DrawString($"Activos: {activos}", encabezadoFont, XBrushes.Black, new XRect(left + 140, top, 140, 12), XStringFormats.TopLeft);
            grafico.DrawString($"Anulados: {anulados}", encabezadoFont, XBrushes.Black, new XRect(left + 280, top, 140, 12), XStringFormats.TopLeft);
            grafico.DrawString($"Pagadas: {pagadas}", encabezadoFont, XBrushes.Black, new XRect(left + 420, top, 140, 12), XStringFormats.TopLeft);
            grafico.DrawString($"Por pagar: {porPagar}", encabezadoFont, XBrushes.Black, new XRect(left + 560, top, 160, 12), XStringFormats.TopLeft);

            // Sumas por categoría (monto)
            var moneyTop = top + 18;
            grafico.DrawString($"Monto total (Bs): {montoTotal.ToString("N2", CultureInfo.InvariantCulture)}", encabezadoFont, XBrushes.Black, new XRect(left, moneyTop, 220, 12), XStringFormats.TopLeft);
            grafico.DrawString($"Monto Activos (Bs): {montoActivos.ToString("N2", CultureInfo.InvariantCulture)}", encabezadoFont, XBrushes.Black, new XRect(left + 220, moneyTop, 220, 12), XStringFormats.TopLeft);
            grafico.DrawString($"Monto Anulados (Bs): {montoAnulados.ToString("N2", CultureInfo.InvariantCulture)}", encabezadoFont, XBrushes.Black, new XRect(left + 440, moneyTop, 220, 12), XStringFormats.TopLeft);

            var moneyTop2 = moneyTop + 14;
            grafico.DrawString($"Monto Pagadas (Bs): {montoPagadas.ToString("N2", CultureInfo.InvariantCulture)}", encabezadoFont, XBrushes.Black, new XRect(left, moneyTop2, 220, 12), XStringFormats.TopLeft);
            grafico.DrawString($"Monto Por pagar (Bs): {montoPorPagar.ToString("N2", CultureInfo.InvariantCulture)}", encabezadoFont, XBrushes.Black, new XRect(left + 220, moneyTop2, 220, 12), XStringFormats.TopLeft);

            y += summaryHeight + 6;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generando resumen PDF: {ex}");
        }

        grafico?.Dispose();
        using var memoria = new MemoryStream();
        documento.Save(memoria, false);
        Response.Headers["Content-Disposition"] = "inline; filename=\"reporte_encomiendas.pdf\"";
        return File(memoria.ToArray(), "application/pdf");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en ExportarReporteEncomiendasPdf: {ex}");
        return StatusCode(500, "Error interno al generar el reporte de encomiendas.");
    }
}


        // GET: api/ticket/reporte-encomiendas/xlsx?clienteRemitenteId=1&recepcionFechaDesde=2026-08-01
        [HttpGet("reporte-encomiendas/xlsx")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportarReporteEncomiendasXlsx(
            [FromQuery] int? clienteRemitenteId, [FromQuery] int? clienteConsignatarioId,
            [FromQuery] string? destino, [FromQuery] bool? estado,
            [FromQuery] string? recepcionFechaDesde, [FromQuery] string? recepcionFechaHasta,
            [FromQuery] string? entregaFechaDesde, [FromQuery] string? entregaFechaHasta,
            [FromQuery] string? numero, [FromQuery] bool? pagado, [FromQuery] int? usuarioId,
            [FromQuery] string? nombreUsuario)
        {
            var resultado = await ObtenerReporteEncomiendas(
                clienteRemitenteId, clienteConsignatarioId, destino, estado,
                recepcionFechaDesde, recepcionFechaHasta, entregaFechaDesde, entregaFechaHasta,
                numero, pagado, usuarioId, nombreUsuario);
            if (resultado.Error != null)
                return BadRequest(resultado.Error);

            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Encomiendas");
            hoja.Range("A1:H2").Merge();
            hoja.Cell("A1").Value = "Reporte detallado de encomiendas";
            hoja.Cell("A1").Style.Font.Bold = true;
            hoja.Cell("A1").Style.Font.FontSize = 16;
            hoja.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg");
            if (System.IO.File.Exists(logoPath))
                hoja.AddPicture(logoPath).MoveTo(hoja.Cell("J1")).WithSize(110, 70);

            hoja.Range("I3:K3").Merge();
            hoja.Range("I4:K4").Merge();
            hoja.Cell("I3").Value = $"Generado por: {nombreUsuario ?? "No especificado"}";
            hoja.Cell("I4").Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
            hoja.Range("I3:K4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            hoja.Range("A5:K5").Merge();
            hoja.Cell("A5").Value = resultado.Filtros.Count == 0
                ? "Criterios: sin filtros"
                : "Criterios: " + string.Join(" | ", resultado.Filtros);
            hoja.Cell("A5").Style.Alignment.WrapText = true;

            var encabezados = new[] { "Número", "Recepción", "Entrega", "Remitente", "Consignatario", "Destino", "Contenido", "Monto (Bs)", "Estado", "Pagado", "Usuario" };
            for (var i = 0; i < encabezados.Length; i++)
                hoja.Cell(7, i + 1).Value = encabezados[i];
            var rangoEncabezado = hoja.Range(7, 1, 7, encabezados.Length);
            rangoEncabezado.Style.Font.Bold = true;
            rangoEncabezado.Style.Fill.BackgroundColor = XLColor.LightGray;

            var fila = 8;
            foreach (var encomienda in resultado.Encomiendas)
            {
                var valores = ValoresEncomienda(encomienda);
                for (var i = 0; i < valores.Length; i++)
                    hoja.Cell(fila, i + 1).Value = valores[i];
                fila++;
            }
            // --- Resumen al final del XLSX ---
            try
            {
                var totalCount = resultado.Encomiendas.Count;
                var activos = resultado.Encomiendas.Count(e => e.Estado == true);
                var anulados = totalCount - activos;
                var pagadas = resultado.Encomiendas.Count(e => e.Pagado == true);
                var porPagar = totalCount - pagadas;
                var montoTotal = resultado.Encomiendas.Where(e => e.Monto.HasValue).Sum(e => e.Monto ?? 0);

                var resumenStart = fila + 1;
                hoja.Range(resumenStart, 1, resumenStart, 3).Merge();
                hoja.Cell(resumenStart, 1).Value = "Resumen";
                hoja.Cell(resumenStart, 1).Style.Font.Bold = true;

                hoja.Cell(resumenStart + 1, 1).Value = "Total encomiendas:";
                hoja.Cell(resumenStart + 1, 2).Value = totalCount;

                hoja.Cell(resumenStart + 2, 1).Value = "Activos:";
                hoja.Cell(resumenStart + 2, 2).Value = activos;

                hoja.Cell(resumenStart + 3, 1).Value = "Anulados:";
                hoja.Cell(resumenStart + 3, 2).Value = anulados;

                hoja.Cell(resumenStart + 4, 1).Value = "Pagadas:";
                hoja.Cell(resumenStart + 4, 2).Value = pagadas;

                hoja.Cell(resumenStart + 5, 1).Value = "Por pagar:";
                hoja.Cell(resumenStart + 5, 2).Value = porPagar;

                hoja.Cell(resumenStart + 6, 1).Value = "Monto total (Bs):";
                hoja.Cell(resumenStart + 6, 2).Value = montoTotal.ToString("N2", CultureInfo.InvariantCulture);

                hoja.Cell(resumenStart + 7, 1).Value = "Monto Activos (Bs):";
                hoja.Cell(resumenStart + 7, 2).Value = resultado.Encomiendas.Where(e => e.Estado == true && e.Monto.HasValue).Sum(e => e.Monto ?? 0).ToString("N2", CultureInfo.InvariantCulture);

                hoja.Cell(resumenStart + 8, 1).Value = "Monto Anulados (Bs):";
                hoja.Cell(resumenStart + 8, 2).Value = resultado.Encomiendas.Where(e => (e.Estado != true) && e.Monto.HasValue).Sum(e => e.Monto ?? 0).ToString("N2", CultureInfo.InvariantCulture);

                hoja.Cell(resumenStart + 9, 1).Value = "Monto Pagadas (Bs):";
                hoja.Cell(resumenStart + 9, 2).Value = resultado.Encomiendas.Where(e => e.Pagado == true && e.Monto.HasValue).Sum(e => e.Monto ?? 0).ToString("N2", CultureInfo.InvariantCulture);

                hoja.Cell(resumenStart + 10, 1).Value = "Monto Por pagar (Bs):";
                hoja.Cell(resumenStart + 10, 2).Value = resultado.Encomiendas.Where(e => (e.Pagado != true) && e.Monto.HasValue).Sum(e => e.Monto ?? 0).ToString("N2", CultureInfo.InvariantCulture);

                // Estilo simple
                hoja.Range(resumenStart + 1, 1, resumenStart + 10, 2).Style.Font.Bold = false;
                hoja.Range(resumenStart + 1, 1, resumenStart + 10, 1).Style.Font.Bold = true;
                fila = resumenStart + 11;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generando resumen XLSX: {ex}");
            }
            if (fila > 8)
                hoja.Range(7, 1, fila - 1, encabezados.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            hoja.Columns(1, encabezados.Length).AdjustToContents();
            hoja.SheetView.FreezeRows(7);

            using var memoria = new MemoryStream();
            libro.SaveAs(memoria);
            return File(memoria.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reporte_encomiendas.xlsx");
        }

        private async Task<ResultadoReporteEncomiendas> ObtenerReporteEncomiendas(
            int? clienteRemitenteId, int? clienteConsignatarioId, string? destino, bool? estado,
            string? recepcionFechaDesde, string? recepcionFechaHasta, string? entregaFechaDesde,
            string? entregaFechaHasta, string? numero, bool? pagado, int? usuarioId, string? nombreUsuario)
        {
            var fechas = new[] { recepcionFechaDesde, recepcionFechaHasta, entregaFechaDesde, entregaFechaHasta };
            if (fechas.Any(fecha => fecha != null && !DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                return new ResultadoReporteEncomiendas { Error = "Las fechas deben tener el formato yyyy-MM-dd." };

            var consulta = _context.Encomienda.AsNoTracking()
                .Include(e => e.ClienteRemitente)
                .Include(e => e.ClienteConsignatario)
                .Include(e => e.Usuario)
                .AsQueryable();

            // Excluir encomiendas que no están pagadas y además no tienen fecha de entrega (null o cadena vacía)
            consulta = consulta.Where(e => !(e.Pagado == false && (e.FechaEntrega == null || e.FechaEntrega == "")));

            if (clienteRemitenteId.HasValue) consulta = consulta.Where(e => e.ClienteRemitenteId == clienteRemitenteId);
            if (clienteConsignatarioId.HasValue) consulta = consulta.Where(e => e.ClienteConsignatarioId == clienteConsignatarioId);
            if (!string.IsNullOrWhiteSpace(destino)) consulta = consulta.Where(e => e.Destino != null && EF.Functions.ILike(e.Destino, $"%{destino.Trim()}%"));
            if (estado.HasValue) consulta = consulta.Where(e => e.Estado == estado);
            if (pagado.HasValue) consulta = consulta.Where(e => e.Pagado == pagado);
            if (usuarioId.HasValue) consulta = consulta.Where(e => e.UsuarioId == usuarioId);
            if (!string.IsNullOrWhiteSpace(numero)) consulta = consulta.Where(e => e.Numero != null && EF.Functions.ILike(e.Numero, $"%{numero.Trim()}%"));
            if (recepcionFechaDesde != null) consulta = consulta.Where(e => e.FechaRecepcion != null && string.Compare(e.FechaRecepcion, recepcionFechaDesde + " 00:00") >= 0);
            if (recepcionFechaHasta != null) consulta = consulta.Where(e => e.FechaRecepcion != null && string.Compare(e.FechaRecepcion, recepcionFechaHasta + " 23:59:59") <= 0);
            if (entregaFechaDesde != null) consulta = consulta.Where(e => e.FechaEntrega != null && e.FechaEntrega != "" && string.Compare(e.FechaEntrega, entregaFechaDesde + " 00:00") >= 0);
            if (entregaFechaHasta != null) consulta = consulta.Where(e => e.FechaEntrega != null && e.FechaEntrega != "" && string.Compare(e.FechaEntrega, entregaFechaHasta + " 23:59:59") <= 0);

            var filtros = new List<string>();
            if (clienteRemitenteId.HasValue)
            {
                var clienteRem = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteRemitenteId.Value);
                filtros.Add($"Cliente remitente: {clienteRem?.NombreCompleto ?? clienteRemitenteId.Value.ToString()}");
            }
            if (clienteConsignatarioId.HasValue)
            {
                var clienteCons = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteConsignatarioId.Value);
                filtros.Add($"Cliente consignatario: {clienteCons?.NombreCompleto ?? clienteConsignatarioId.Value.ToString()}");
            }
            if (!string.IsNullOrWhiteSpace(destino)) filtros.Add($"Destino: {destino.Trim()}");
            if (estado.HasValue) filtros.Add($"Estado: {(estado.Value ? "Activo" : "Anulado")}");
            if (recepcionFechaDesde != null) filtros.Add($"Recepción desde: {recepcionFechaDesde}");
            if (recepcionFechaHasta != null) filtros.Add($"Recepción hasta: {recepcionFechaHasta}");
            if (entregaFechaDesde != null) filtros.Add($"Entrega desde: {entregaFechaDesde}");
            if (entregaFechaHasta != null) filtros.Add($"Entrega hasta: {entregaFechaHasta}");
            if (!string.IsNullOrWhiteSpace(numero)) filtros.Add($"Número: {numero.Trim()}");
            if (pagado.HasValue) filtros.Add($"Pagado: {(pagado.Value ? "Sí" : "No")}");
            if (usuarioId.HasValue)
            {
                var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId.Value);
                filtros.Add($"Usuario: {usuario?.Usuario1 ?? usuarioId.Value.ToString()}");
            }

            return new ResultadoReporteEncomiendas
            {
                Encomiendas = await consulta.OrderByDescending(e => e.Id).ToListAsync(),
                Filtros = filtros
            };
        }

        private static string[] ValoresEncomienda(Encomiendum e) =>
        [e.Numero ?? string.Empty, e.FechaRecepcion ?? string.Empty, e.FechaEntrega ?? string.Empty,
         e.ClienteRemitente?.NombreCompleto ?? string.Empty, e.ClienteConsignatario?.NombreCompleto ?? string.Empty,
         e.Destino ?? string.Empty, e.Contenido ?? string.Empty, e.Monto?.ToString("N2", CultureInfo.InvariantCulture) ?? string.Empty,
         e.Estado == true ? "Activo" : "Anulado", e.Pagado == true ? "pagada" : "por pagar", e.Usuario?.Usuario1 ?? string.Empty];

        private static (string Nombre, double Ancho, int Indice)[] ColumnasPdf() =>
        [("Número", 52, 0), ("Recepción", 67, 1), ("Entrega", 67, 2), ("Remitente", 78, 3),
         ("Consignatario", 78, 4), ("Destino", 55, 5), ("Contenido", 95, 6), ("Monto Bs", 52, 7),
         ("Estado", 48, 8), ("Pagado", 45, 9), ("Usuario", 55, 10)];

        private sealed class ResultadoReporteEncomiendas
        {
            public List<Encomiendum> Encomiendas { get; set; } = [];
            public List<string> Filtros { get; set; } = [];
            public string? Error { get; set; }
        }
        private static (string N, double W, int I)[] Cols() =>
[
    ("N°", 30, 0),
    ("ID", 40, 1),
    ("Fecha/Hora", 80, 2),
    ("Destino", 65, 3),
    ("Móvil", 48, 4),
    ("Monto (Bs)", 55, 5),
    ("Estado", 45, 6),
    ("Reserva", 45, 7),
    ("Cliente", 90, 8),
    ("Teléfono", 68, 9),
    ("Asiento", 45, 10),
    ("Usuario", 55, 11)
];
        private static string[] Values(Pasaje p, int index) =>
        [
            index.ToString(),
    p.Id.ToString(),
    p.FechaHora ?? "",
    p.Destino ?? "",
    p.Movil ?? "",
    p.Monto?.ToString("N2", CultureInfo.InvariantCulture) ?? "",
    p.Estado == true ? "Activo" : "Anulado",
    p.Reserva == true ? "Sí" : "No",
    p.Cliente?.NombreCompleto ?? "",
    p.Cliente?.Telefono ?? "",
    p.Asiento?.Numero?.ToString() ?? "",
    p.Usuario?.Usuario1 ?? ""
        ];

    }
}

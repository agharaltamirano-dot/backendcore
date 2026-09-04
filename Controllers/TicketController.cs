using Microsoft.AspNetCore.Mvc;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketController : ControllerBase
    {
        private readonly TransporteContext _context;

        public TicketController(TransporteContext context)
        {
            _context = context;
        }

        // GET: api/ticket/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTicket(int id)
        {
            try
            {
                System.Console.WriteLine("----- RAW TICKET RECEIVED -----");

                // La autorización la maneja el middleware [Authorize]

                var p = await _context.Pasajes
                    .Include(pa => pa.Cliente)
                    .Include(pa => pa.Asiento)
                    .Include(pa => pa.Horario).ThenInclude(h => h.Ruta)
                    .Include(pa => pa.Horario).ThenInclude(h => h.Vehiculo)
                    .FirstOrDefaultAsync(pa => pa.Id == id);

                if (p == null) return NotFound();

                // Crear PDF tamaño aproximado a rollo de 80mm (ancho ~ 80mm)
                using var doc = new PdfDocument();
                var page = doc.AddPage();
                page.Width = XUnit.FromMillimeter(80); // ancho 80mm
                page.Height = XUnit.FromMillimeter(200); // altura inicial

                var gfx = XGraphics.FromPdfPage(page);
                var labelFont = new XFont("Arial", 9, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
                var valueFont = new XFont("Arial", 9, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
                double y = 10;
                double left = 10;
                double pageWidth = page.Width.Point;
                double pageHeight = page.Height.Point;

                void EnsureNewPageIfNeeded()
                {
                    if (y + 14 > pageHeight - 10)
                    {
                        var newPage = doc.AddPage();
                        newPage.Width = XUnit.FromMillimeter(80);
                        newPage.Height = XUnit.FromMillimeter(200);
                        pageHeight = newPage.Height.Point;
                        pageWidth = newPage.Width.Point;
                        gfx.Dispose();
                        gfx = XGraphics.FromPdfPage(newPage);
                        y = 10;
                    }
                }

                void DrawPair(string label, string value)
{
    EnsureNewPageIfNeeded();
    double labelWidth = 50;                 // ancho más reducido para el label
    double valueLeft = left + labelWidth; // solo 2pt de separación
    double valueWidth = pageWidth - valueLeft - 30; // margen derecho de 15pt

    // dibujar label (sin margen extra)
    gfx.DrawString(label, labelFont, XBrushes.Black,
        new XRect(left, y, labelWidth, 20), XStringFormats.TopLeft);

    // dibujar value (alineado a la derecha dentro de su rectángulo)
    var textValue = value ?? string.Empty;
    gfx.DrawString(textValue, valueFont, XBrushes.Black,
        new XRect(valueLeft, y, valueWidth, 20), XStringFormats.TopRight);

    y += 14;
}

// cargar la imagen desde tu carpeta assets
var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg");
if (System.IO.File.Exists(logoPath))
{
    var logo = XImage.FromFile(logoPath);

    // calcular posición centrada
    double logoWidth = 60;   // ancho deseado en puntos
    double logoHeight = 60;  // alto deseado en puntos
    double logoX = (pageWidth - logoWidth) / 2; // centrado horizontal
    double logoY = y;        // posición vertical actual

    gfx.DrawImage(logo, logoX, logoY, logoWidth, logoHeight);

    y += (int)logoHeight + 5; // avanzar la coordenada Y para que no se superponga con el texto
}

                // encabezado centrado + subtítulo centrado debajo
                EnsureNewPageIfNeeded();
                var headerFont = new XFont("Arial", 11, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
                var header = "SINDICATO MIXTO";
                var headerWidth = gfx.MeasureString(header, headerFont).Width;
                gfx.DrawString(header, headerFont, XBrushes.Black, new XRect((pageWidth - headerWidth) / 2, y, headerWidth, 20), XStringFormats.TopLeft);
                y += 16;

                // subtítulo centrado
                var subtitleFont = new XFont("Arial", 9, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
                var subtitle = "RIO SAN JUAN DEL ORO";
                var subtitleWidth = gfx.MeasureString(subtitle, subtitleFont).Width;
                gfx.DrawString(subtitle, subtitleFont, XBrushes.Black, new XRect((pageWidth - subtitleWidth) / 2, y, subtitleWidth, 16), XStringFormats.TopLeft);
                y += 12;

                // DrawPair("Pasaje ID:", p.Id.ToString());
                DrawPair("FechaHora:", p.FechaHora.ToString(CultureInfo.InvariantCulture));
                DrawPair("Destino:", p.Destino);
                DrawPair("Movil:", p.Movil);
                DrawPair("Asiento:", p.Asiento?.Numero?.ToString());

                EnsureNewPageIfNeeded();
                gfx.DrawString("-----------------------------------------------------------", valueFont, XBrushes.Black, new XRect(left, y, pageWidth - 20, 20), XStringFormats.TopLeft);
                y += 14;

                // datos del cliente y monto debajo de la franja
                DrawPair("Cliente:", p.Cliente?.NombreCompleto);
                // mostrar CI y Teléfono (labels siempre presentes, valores sólo si están)
                DrawPair("CI:", p.Cliente?.Ci);
                DrawPair("Teléfono:", p.Cliente?.Telefono);

                // Monto: manejar nullable y aplicar formato
                string precioText;
                if (p.Monto == null)
                    precioText = string.Empty;
                else
                    precioText = p.Monto.Value.ToString("N2", CultureInfo.InvariantCulture) + " Bs";

                DrawPair("Precio:", precioText);

                using var ms = new MemoryStream();
                doc.Save(ms, false);
                ms.Position = 0;

                var pdfBytes = ms.ToArray();

                // Establecer cabeceras: Content-Type y Content-Disposition inline
                Response.Headers["Content-Type"] = "application/pdf";
                Response.Headers["Content-Disposition"] = $"inline; filename=\"pasaje_{p.Id}.pdf\"";

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                // registrar error y responder 500
                System.Console.Error.WriteLine(ex);
                return StatusCode(500, "Error generando el ticket");
            }
        }

        // GET: api/horarioHojaRuta/{id}
        [HttpGet("horarioHojaRuta/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHojaRuta(int id)
        {
            try
            {
                var h = await _context.Horarios
                    .Include(x => x.Ruta).ThenInclude(r => r.Destinos).ThenInclude(d => d.PuntoVenta)
                    .Include(x => x.Vehiculo).ThenInclude(v => v.Conductor)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (h == null) return NotFound();

                using var doc = new PdfDocument();
                var page = doc.AddPage();
                page.Width = XUnit.FromMillimeter(80);
                page.Height = XUnit.FromMillimeter(300);

                var gfx = XGraphics.FromPdfPage(page);
                var headerFont = new XFont("Arial", 12, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
                var subtitleFont = new XFont("Arial", 10, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
                var labelFont = new XFont("Arial", 9, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
                var valueFont = new XFont("Arial", 9, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));

                double y = 10;
                double left = 10;
                double pageWidth = page.Width.Point;
                double pageHeight = page.Height.Point;

                void EnsureNewPageIfNeeded()
                {
                    if (y + 14 > pageHeight - 10)
                    {
                        var newPage = doc.AddPage();
                        newPage.Width = XUnit.FromMillimeter(80);
                        newPage.Height = XUnit.FromMillimeter(300);
                        pageHeight = newPage.Height.Point;
                        pageWidth = newPage.Width.Point;
                        gfx.Dispose();
                        gfx = XGraphics.FromPdfPage(newPage);
                        y = 10;
                    }
                }

                void DrawPairLocal(string label, string value)
                {
                    EnsureNewPageIfNeeded();
                    double labelWidth = 40;
                    double valueLeft = left + labelWidth;
                    double valueWidth = pageWidth - valueLeft - 30;
                    gfx.DrawString(label, labelFont, XBrushes.Black, new XRect(left, y, labelWidth, 20), XStringFormats.TopLeft);
                    gfx.DrawString(value ?? string.Empty, valueFont, XBrushes.Black, new XRect(valueLeft, y, valueWidth, 20), XStringFormats.TopRight);
                    y += 14;
                }
                // cargar la imagen desde tu carpeta assets
var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "logo3.jpeg");
if (System.IO.File.Exists(logoPath))
{
    var logo = XImage.FromFile(logoPath);

    // calcular posición centrada
    double logoWidth = 60;   // ancho deseado en puntos
    double logoHeight = 60;  // alto deseado en puntos
    double logoX = (pageWidth - logoWidth) / 2; // centrado horizontal
    double logoY = y;        // posición vertical actual

    gfx.DrawImage(logo, logoX, logoY, logoWidth, logoHeight);

    y += (int)logoHeight + 5; // avanzar la coordenada Y para que no se superponga con el texto
}

                // Header
                var title = "HOJA DE RUTA";
                var titleW = gfx.MeasureString(title, headerFont).Width;
                gfx.DrawString(title, headerFont, XBrushes.Black, new XRect((pageWidth - titleW) / 2, y, titleW, 20), XStringFormats.TopLeft);
                y += 18;

                // Subtitulo
                var sub = "RIO SAN JUAN DEL ORO";
                var subW = gfx.MeasureString(sub, subtitleFont).Width;
                gfx.DrawString(sub, subtitleFont, XBrushes.Black, new XRect((pageWidth - subW) / 2, y, subW, 18), XStringFormats.TopLeft);
                y += 18;

                // Origen y Destino: origen = destino.Orden == 1, destino = max orden
                string origen = null;
                string destino = null;
                if (h.Ruta?.Destinos != null)
                {
                    var originDest = h.Ruta.Destinos.FirstOrDefault(d => d.Orden == 1);
                    origen = originDest?.PuntoVenta?.Nombre;
                    var last = h.Ruta.Destinos.OrderByDescending(d => d.Orden).FirstOrDefault();
                    destino = last?.PuntoVenta?.Nombre;
                }

                DrawPairLocal("Horario:", h.Id.ToString());
                DrawPairLocal("Fecha:", h.Fecha.ToString());
                DrawPairLocal("Hora:", h.Hora);
                DrawPairLocal("Origen:", origen);
                DrawPairLocal("Destino:", destino);
                DrawPairLocal("Movil:", h.Vehiculo?.Movil);
                DrawPairLocal("Conductor:", (h.Vehiculo?.Conductor?.Nombres ?? string.Empty) + " " + (h.Vehiculo?.Conductor?.Apellidos ?? string.Empty));

                // Listado de pasajes: 3 columnas (Nombre, CI, Teléfono)
                var pasajes = await _context.Pasajes
                    .Include(pa => pa.Cliente)
                    .Include(pa => pa.Asiento)
                    .Where(pa => pa.HorarioId == h.Id  && pa.Estado == true 
                 && pa.Reserva == false)
                    .ToListAsync();

                EnsureNewPageIfNeeded();
                double tableLeft = left;
                double tableRight = pageWidth - 10;
                double tableWidth = tableRight - tableLeft;
                double colWidth = tableWidth / 4.0;

                // encabezados de columna
                //primero el asiento
                gfx.DrawString("Asiento", labelFont, XBrushes.Black, new XRect(tableLeft, y, colWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString("Nombre", labelFont, XBrushes.Black, new XRect(tableLeft + colWidth, y, colWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString("CI", labelFont, XBrushes.Black, new XRect(tableLeft + 2 * colWidth, y, colWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString("Teléfono", labelFont, XBrushes.Black, new XRect(tableLeft + 3 * colWidth, y, colWidth, 20), XStringFormats.TopLeft);
                y += 14;

                // filas de datos
                foreach (var pa in pasajes)
                {
                    EnsureNewPageIfNeeded();
                    var nombre = pa.Cliente?.NombreCompleto ?? string.Empty;
                    var ci = pa.Cliente?.Ci ?? string.Empty;
                    var tel = pa.Cliente?.Telefono ?? string.Empty;

                    gfx.DrawString(pa.Asiento?.Numero.ToString(), valueFont, XBrushes.Black, new XRect(tableLeft, y, colWidth, 20), XStringFormats.TopLeft);
                    gfx.DrawString(nombre, valueFont, XBrushes.Black, new XRect(tableLeft + colWidth, y, colWidth, 20), XStringFormats.TopLeft);
                    gfx.DrawString(ci, valueFont, XBrushes.Black, new XRect(tableLeft + 2 * colWidth, y, colWidth, 20), XStringFormats.TopLeft);
                    gfx.DrawString(tel, valueFont, XBrushes.Black, new XRect(tableLeft + 3 * colWidth, y, colWidth, 20), XStringFormats.TopLeft);
                    y += 14;
                }

                // espacio y franja separadora
                y += 6;
                EnsureNewPageIfNeeded();
                gfx.DrawString("-----------------------------------------------------------", valueFont, XBrushes.Black, new XRect(left, y, pageWidth - 20, 20), XStringFormats.TopLeft);
                y += 14;

                using var ms = new MemoryStream();
                doc.Save(ms, false);
                ms.Position = 0;
                var pdfBytes = ms.ToArray();

                Response.Headers["Content-Type"] = "application/pdf";
                Response.Headers["Content-Disposition"] = $"inline; filename=\"hojaRuta_{h.Id}.pdf\"";
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                return StatusCode(500, "Error generando hoja de ruta");
            }
        }

    }
}

using SEDESLABORATORIO.Application.Metricas;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;
[Authorize(Roles = "gerente")]
public class MetricasController(IConsultaMetricas consulta, IExportadorMetricas exportador, IReloj reloj) : Controller
{
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, [System.ComponentModel.DataAnnotations.StringLength(100), SEDESLABORATORIO.Infrastructure.TextoSeguro] string? municipio, bool descargar = false)
        => await Consultar(desde, hasta, municipio, descargar, false);

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Datos(DateTime? desde, DateTime? hasta, [System.ComponentModel.DataAnnotations.StringLength(100), SEDESLABORATORIO.Infrastructure.TextoSeguro] string? municipio)
        => await Consultar(desde, hasta, municipio, false, true);

    private async Task<IActionResult> Consultar(DateTime? desde, DateTime? hasta, [System.ComponentModel.DataAnnotations.StringLength(100), SEDESLABORATORIO.Infrastructure.TextoSeguro] string? municipio, bool descargar, bool json)
    {
        var start = (desde ?? reloj.UtcNow.AddMonths(-1)).Date;
        var end = (hasta ?? reloj.UtcNow).Date;
        if (!ModelState.IsValid || end < start || end >= DateTime.MaxValue.Date || (end - start).TotalDays > 3660)
        {
            if (json || descargar) return BadRequest("El rango debe ser válido y no superar 10 años.");
            Response.StatusCode = 400;
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd"); ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
            return View("FiltroInvalido");
        }
        var resultado = await consulta.Consultar(start, end, municipio);
        if (descargar) return File(exportador.Exportar(resultado), "text/csv; charset=utf-8", "metricas.csv");
        if (json) return Json(resultado.Visuales);
        return View("Index", resultado);
    }
}

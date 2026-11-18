using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace SEDESLABORATORIO.SolicitudSeguimiento.Controllers;
[Area("solicitud-seguimiento"), Authorize(Roles = "propietario")]
public class SolicitudSeguimientoController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Gestion", new { area = "" });
    [HttpGet]
    public IActionResult Details(int id) => RedirectToAction("Detalle", "Gestion", new { area = "", id });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Enviar(int id) => RedirectToAction("Detalle", "Gestion", new { area = "", id });
}

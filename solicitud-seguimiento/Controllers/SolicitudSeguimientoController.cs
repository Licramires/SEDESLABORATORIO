using Microsoft.AspNetCore.Mvc;

namespace SEDESLABORATORIO.SolicitudSeguimiento.Controllers;

[Area("solicitud-seguimiento")]
public class SolicitudSeguimientoController : Controller
{
    public IActionResult Index() => View();
}

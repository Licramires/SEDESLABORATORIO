using Microsoft.AspNetCore.Mvc;

namespace SEDESLABORATORIO.SolicitudRegistro.Controllers;

[Area("solicitud-registro")]
public class SolicitudRegistroController : Controller
{
    public IActionResult Index() => View();
}

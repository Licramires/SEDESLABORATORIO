using Microsoft.AspNetCore.Mvc;

namespace SEDESLABORATORIO.Publico.Controllers;

[Area("publico")]
public class PublicoController : Controller
{
    public IActionResult Index() => View();
}

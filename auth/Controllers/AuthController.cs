using Microsoft.AspNetCore.Mvc;

namespace SEDESLABORATORIO.Auth.Controllers;

[Area("auth")]
public class AuthController : Controller
{
    public IActionResult Index() => View();
}

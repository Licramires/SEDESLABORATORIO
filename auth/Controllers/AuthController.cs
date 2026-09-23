using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEDESLABORATORIO.Auth.Models;
using SEDESLABORATORIO.Auth.Services;

namespace SEDESLABORATORIO.Auth.Controllers;

[Area("auth")]
public class AuthController : Controller
{
    private const string AuthenticationScheme = "SiLabCookie";
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToAction(nameof(Dashboard))
            : RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ValidateCredentialsAsync(
            model.Email,
            model.Password,
            cancellationToken);

        if (!result.Succeeded || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "No fue posible iniciar sesión.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new(ClaimTypes.Name, result.User.Nombre),
            new(ClaimTypes.Email, result.User.Email),
            new(ClaimTypes.Role, result.User.Rol.ToString().ToLowerInvariant())
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        await HttpContext.SignInAsync(
            AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                AllowRefresh = true
            });

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    [Authorize]
    public IActionResult Dashboard()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}

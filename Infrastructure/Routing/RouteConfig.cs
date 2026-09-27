using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace SEDESLABORATORIO.Infrastructure.Routing;

public static class RouteConfig
{
    public static IEndpointRouteBuilder MapApplicationRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Index}/{action=Index}/{id?}");

        // Rutas del módulo auth van aquí.
        // Rutas del módulo publico van aquí.
        endpoints.MapControllerRoute(
            name: "publico-home",
            pattern: "",
            defaults: new { area = "publico", controller = "Publico", action = "Index" });

        endpoints.MapControllerRoute(
            name: "publico",
            pattern: "laboratorios/{action=Index}/{id?}",
            defaults: new { area = "publico", controller = "Publico" });

        // Rutas del módulo solicitud-registro van aquí.
        // Rutas del módulo solicitud-seguimiento van aquí.

        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return endpoints;
    }
}

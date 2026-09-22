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
        // Rutas del módulo solicitud-registro van aquí.
        // Rutas del módulo solicitud-seguimiento van aquí.

        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return endpoints;
    }
}

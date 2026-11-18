using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

// Las navegaciones de formularios conservan el código HTTP y ofrecen un GET
// seguro de recuperación. Las consultas JSON y clientes HTTP no cambian formato.
public sealed class RecuperacionOperacionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (context.Controller is not Controller controller ||
            context.ActionDescriptor is not ControllerActionDescriptor descriptor ||
            descriptor.ControllerName is not ("Gestion" or "Administracion" or "SolicitudRegistro") ||
            !context.HttpContext.Request.Headers.Accept.Any(v => v?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true) ||
            executed.Result is not ObjectResult { StatusCode: 400 or 409, Value: string mensaje } error)
            return;

        int? id = null;
        foreach (var argument in context.ActionArguments)
        {
            id = argument.Value switch
            {
                DecisionModel m => m.Id,
                RevisionDocumentoForm m => m.Id,
                CitacionForm m => m.Id,
                DatosBorradorForm m => m.Id,
                int value when argument.Key.Equals("id", StringComparison.OrdinalIgnoreCase) => value,
                _ => id
            };
        }
        var expediente = descriptor.ControllerName == "Gestion" && id > 0;
        var returnUrl = expediente
            ? controller.Url.Action("Detalle", "Gestion", new { area = "", id })!
            : descriptor.ControllerName == "Administracion"
                ? controller.Url.Action("Index", "Auth", new { area = "auth" })!
                : controller.Url.Action("Index", "Gestion", new { area = "" })!;
        var model = new OperacionFallidaModel(mensaje, error.StatusCode == 409,
            returnUrl, expediente ? "Ver versión actual del trámite" : "Volver a mis tareas");
        executed.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/OperacionFallida.cshtml",
            StatusCode = error.StatusCode,
            ViewData = new ViewDataDictionary<OperacionFallidaModel>(controller.ViewData, model),
            TempData = controller.TempData
        };
    }
}

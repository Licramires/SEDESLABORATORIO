using SEDESLABORATORIO.Application.Tramites;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

public record PermisoDefinicion(RolUsuario Rol, string Clave, string Descripcion);
public static class Permisos
{
    public static readonly PermisoDefinicion[] Catalogo =
    [
        new(RolUsuario.Propietario, "solicitudes", "Crear solicitudes, adjuntar documentos y enviarlos"),
        new(RolUsuario.Propietario, "establecimientos", "Editar servicios y solicitar renovaciones"),
        new(RolUsuario.Coordinador, "documentos", "Aprobar o rechazar documentación"),
        new(RolUsuario.Coordinador, "asignaciones", "Asignar y reprogramar inspecciones"),
        new(RolUsuario.Coordinador, "actas", "Validar o rechazar actas"),
        new(RolUsuario.Coordinador, "informes", "Descargar informes de cierre"),
        new(RolUsuario.Supervisor, "inspecciones", "Agenda, formularios y registro de actas"),
        new(RolUsuario.Abogado, "legal", "Revisión legal y habilitación"),
        new(RolUsuario.Gerente, "metricas", "Consultar y exportar métricas"),
        new(RolUsuario.Administrador, "usuarios", "Administrar usuarios"),
        new(RolUsuario.Administrador, "requisitos", "Administrar requisitos")
    ];
}

// Complementa las restricciones por rol; no concede atribuciones fuera de cada perfil.
public sealed class PermisoFilter(IPermisosDatos db, RegistroAccionesTramite acciones) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<RolUsuario>(role, true, out var rol)) { await next(); return; }
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controller = descriptor?.ControllerName;
        var action = descriptor?.ActionName;
        string? key = controller switch
        {
            "SolicitudRegistro" => "solicitudes",
            "Metricas" => "metricas",
            "Administracion" when action is "Usuarios" or "Usuario" => "usuarios",
            "Administracion" when action is "Requisito" or "Requisitos" or "EliminarRequisito" or "EstadoRequisito" or "Seccion" or "EstadoSeccion" or "EliminarSeccion" or "Catalogos" or "Catalogo" => "requisitos",
            "Administracion" when action is "CalidadDatos" => "usuarios",
            "Gestion" when action is "EditarDatos" or "Documento" or "AsociarDocumento" or "RetirarComplementario" or "RetirarDocumento" => "solicitudes",
            "Gestion" when action is "RevisarDocumento" => "documentos",
            "Gestion" when action is "Establecimientos" or "Establecimiento" or "Servicios" or "Renovar" => "establecimientos",
            "Gestion" when action is "Agenda" or "Rutas" or "Actas" or "FormularioActa" or "PrepararActa" or "PlantillaCitacion" or "Citacion" or "Citaciones" && rol == RolUsuario.Supervisor => "inspecciones",
            "Gestion" when action == "Informe" => "informes",
            _ => null
        };
        if (controller == "Gestion" && action == "Decidir" && context.ActionArguments.TryGetValue("model", out var arg) && arg is DecisionModel model)
            key = acciones.Buscar(model.Accion)?.Permiso;
        if (key is not null && await db.PermisosRol.AnyAsync(p => p.Rol == rol && p.Clave == key && !p.Habilitado))
        { context.Result = new ForbidResult(); return; }
        await next();
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;

public partial class AdministracionController
{
    [HttpGet]
    public async Task<IActionResult> Roles() => View(new RolesAdminModel
    {
        Permisos = await db.PermisosRol.AsNoTracking().ToListAsync(),
        Usuarios = await db.Usuarios.GroupBy(u => u.Rol).ToDictionaryAsync(g => g.Key, g => g.Count()),
        Cambios = await db.CambiosPermisos.AsNoTracking().OrderByDescending(c => c.Id).Take(30).ToListAsync()
    });

    [HttpGet]
    public async Task<IActionResult> EditarPermisos(RolUsuario rol)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(rol)) return BadRequest();
        var actuales = await db.PermisosRol.AsNoTracking().Where(p => p.Rol == rol).ToListAsync();
        return View(new PermisosForm { Rol = rol, Permisos = Permisos.Catalogo.Where(p => p.Rol == rol).Select(p =>
        {
            var actual = actuales.SingleOrDefault(a => a.Clave == p.Clave);
            return new PermisoFila { Clave = p.Clave, Habilitado = actual?.Habilitado ?? true, Version = actual?.Version ?? 0 };
        }).ToList() });
    }

    [HttpPost]
    public async Task<IActionResult> EditarPermisos(PermisosForm model)
    {
        var validas = Permisos.Catalogo.Where(p => p.Rol == model.Rol).Select(p => p.Clave).Order().ToArray();
        if (!ModelState.IsValid || validas.Length == 0 || !model.Permisos.Select(p => p.Clave).Order().SequenceEqual(validas))
            return BadRequest("Envíe la lista completa de permisos del perfil seleccionado.");
        await using var transaction = await db.BeginTransactionAsync();
        foreach (var fila in model.Permisos)
        {
            var actual = await db.PermisosRol.FindAsync(model.Rol, fila.Clave);
            if ((actual?.Version ?? 0) != fila.Version) return Conflict("Los permisos cambiaron. Recargue la página antes de guardar.");
            ActualizarPermiso(model.Rol, fila.Clave, fila.Habilitado, actual);
        }
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateException) { return Conflict("Los permisos cambiaron. Recargue la página antes de guardar."); }
        TempData["Success"] = "Permisos guardados. Los cambios se aplican inmediatamente, incluso a las sesiones abiertas.";
        return RedirectToAction(nameof(Roles));
    }

    // Compatibilidad con acciones anteriores; usa la misma persistencia y registro de cambios.
    [HttpPost]
    public async Task<IActionResult> Permiso([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] RolUsuario rol, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] string clave, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] bool habilitado)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        if (!Permisos.Catalogo.Any(p => p.Rol == rol && p.Clave == clave)) return BadRequest("Permiso inválido para ese perfil.");
        ActualizarPermiso(rol, clave, habilitado, await db.PermisosRol.FindAsync(rol, clave));
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("Los permisos cambiaron. Recargue la página."); }
        TempData["Success"] = "Permiso actualizado. Se aplica inmediatamente a las sesiones existentes.";
        return RedirectToAction(nameof(Roles));
    }

    private void ActualizarPermiso(RolUsuario rol, string clave, bool habilitado, PermisoRol? actual)
    {
        var antes = actual?.Habilitado ?? true;
        if (actual is null) { actual = new PermisoRol { Rol = rol, Clave = clave }; db.PermisosRol.Add(actual); }
        else if (antes != habilitado) actual.Version++;
        actual.Habilitado = habilitado;
        if (antes != habilitado) db.CambiosPermisos.Add(new CambioPermiso { Rol = rol, Clave = clave, Habilitado = habilitado,
            Actor = $"{User.Identity?.Name ?? "Administrador"} (#{User.FindFirstValue(ClaimTypes.NameIdentifier)})" });
    }
}

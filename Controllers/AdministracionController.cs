using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Models;
using SEDESLABORATORIO.Infrastructure;

namespace SEDESLABORATORIO.Controllers;

[Authorize(Roles = "administrador"), AutoValidateAntiforgeryToken]
public partial class AdministracionController(IAdministracionDatos db, IAlmacenPdf storage, IPasswordHasher<Usuario> passwordHasher) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Usuarios() => View(await db.Usuarios.AsNoTracking().OrderBy(u => u.Nombre).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Usuario(int? id)
    {
        if (!ModelState.IsValid) return BadRequest("Identificador inválido.");
        if (id is null) return View(new UsuarioForm());
        var u = await db.Usuarios.FindAsync(id); if (u is null) return NotFound();
        return View(new UsuarioForm { Id = u.Id, Version = u.Version, Nombre = u.Nombre, Email = u.Email, Rol = u.Rol, Activo = u.Activo });
    }

    [HttpPost]
    public async Task<IActionResult> Usuario(UsuarioForm model)
    {
        await using var transaction = await db.BeginTransactionAsync();
        if (model.Id == 0 && string.IsNullOrWhiteSpace(model.Password)) ModelState.AddModelError(nameof(model.Password), "Ingrese una contraseña de al menos 10 caracteres.");
        var email = (model.Email ?? "").Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == email && u.Id != model.Id)) ModelState.AddModelError(nameof(model.Email), "El correo ya está registrado.");
        var actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (model.Id == actor && (!model.Activo || model.Rol != RolUsuario.Administrador)) ModelState.AddModelError("", "No puede quitar su propio acceso de administrador.");
        var user = model.Id == 0 ? new Usuario() : await db.Usuarios.FindAsync(model.Id);
        if (user is null) return NotFound();
        if (user.Id != 0 && user.Version != model.Version) return Conflict("El usuario cambió. Recargue antes de editarlo.");
        if ((!model.Activo || model.Rol != RolUsuario.Supervisor) && user.Rol == RolUsuario.Supervisor &&
            await db.Inspecciones.AnyAsync(i => i.SupervisorId == user.Id && i.Estado == "pendiente"))
            ModelState.AddModelError("", "Reasigne las inspecciones pendientes antes de desactivar o cambiar el rol del supervisor.");
        if (!ModelState.IsValid) return View(model);
        user.Nombre = model.Nombre.Trim(); user.Email = email; user.Rol = model.Rol; user.Activo = model.Activo;
        if (!string.IsNullOrEmpty(model.Password)) user.Password = passwordHasher.HashPassword(user, model.Password);
        user.SelloSeguridad = Guid.NewGuid().ToString("N");
        if (user.Id != 0) user.Version++;
        if (user.Id == 0) db.Usuarios.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("El usuario cambió. Recargue antes de editarlo."); }
        catch (DbUpdateException) { ModelState.AddModelError("", "No se pudo guardar; compruebe que el correo no esté duplicado."); return View(model); }
        await transaction.CommitAsync();
        TempData["Success"] = "Usuario guardado. Los cambios de rol o desactivación invalidan su sesión.";
        return RedirectToAction(nameof(Usuarios));
    }

}

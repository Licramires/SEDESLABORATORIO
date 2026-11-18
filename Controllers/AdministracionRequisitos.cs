using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;

public partial class AdministracionController
{
    [HttpGet]
    public async Task<IActionResult> Requisitos() => View(new RequisitosAdminModel
    {
        Secciones = await db.SeccionesRequisitos.AsNoTracking().Where(s => !s.Eliminada).OrderBy(s => s.Orden).ThenBy(s => s.Id).ToListAsync(),
        Requisitos = await db.Requisitos.AsNoTracking().Where(r => !r.Eliminado).OrderBy(r => r.Orden).ThenBy(r => r.Id).ToListAsync()
    });

    private async Task PrepararSecciones() => ViewBag.Secciones = new SelectList(
        await db.SeccionesRequisitos.AsNoTracking().Where(s => !s.Eliminada).OrderBy(s => s.Orden).ThenBy(s => s.Id).ToListAsync(), "Id", "Nombre");

    [HttpGet]
    public async Task<IActionResult> Requisito(int? id, int? seccionId)
    {
        if (!ModelState.IsValid) return BadRequest("Identificador inválido.");
        await PrepararSecciones();
        if (id is null) return View(new RequisitoForm { SeccionId = seccionId });
        var r = await db.Requisitos.SingleOrDefaultAsync(r => r.Id == id && !r.Eliminado);
        if (r is null) return NotFound();
        return View(new RequisitoForm { Id = r.Id, Nombre = r.Nombre, Obligatorio = r.Obligatorio, Activo = r.Activo,
            Version = r.Version, SeccionId = r.SeccionId, Orden = r.Orden });
    }

    [HttpPost, RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Requisito(RequisitoForm? model)
    {
        if (model is null) return BadRequest("No se pudo leer el formulario.");
        await using var transaction = await db.BeginTransactionAsync();
        await PrepararSecciones();
        if (model.SeccionId.HasValue && !await db.SeccionesRequisitos.AnyAsync(s => s.Id == model.SeccionId && !s.Eliminada))
            ModelState.AddModelError(nameof(model.SeccionId), "Seleccione una sección disponible.");
        if (model.Plantilla is not null)
        {
            var error = await PdfStorage.ValidateAsync(model.Plantilla);
            if (error is not null) ModelState.AddModelError(nameof(model.Plantilla), error);
        }
        if (!ModelState.IsValid) return View(model);
        var r = model.Id == 0 ? new Requisito() : await db.Requisitos.SingleOrDefaultAsync(r => r.Id == model.Id && !r.Eliminado);
        if (r is null) return NotFound();
        if (r.Id != 0 && r.Version != model.Version) return Conflict("El requisito cambió. Recargue la página.");
        r.Nombre = model.Nombre.Trim(); r.Obligatorio = model.Obligatorio; r.Activo = model.Activo;
        r.SeccionId = model.SeccionId; r.Orden = model.Orden;
        if (r.Id != 0) r.Version++;
        string? key = null;
        if (model.Plantilla is not null) { key = await storage.SaveAsync(model.Plantilla); r.PlantillaPdf = key; }
        if (r.Id == 0) db.Requisitos.Add(r);
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateConcurrencyException) { if (key is not null) storage.Delete(key); return Conflict("El requisito cambió. Recargue la página."); }
        catch { if (key is not null) storage.Delete(key); throw; }
        TempData["Success"] = "Requisito guardado. Los expedientes enviados conservan su referencia anterior.";
        return RedirectToAction(nameof(Requisitos));
    }

    [HttpPost]
    public async Task<IActionResult> EstadoRequisito([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] bool activo)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        var r = await db.Requisitos.SingleOrDefaultAsync(r => r.Id == id && !r.Eliminado);
        if (r is null) return NotFound();
        if (r.Version != version) return Conflict("El requisito cambió. Recargue la página.");
        r.Activo = activo; r.Version++;
        return await GuardarCatalogo("Estado del requisito actualizado.");
    }

    [HttpPost]
    public async Task<IActionResult> EliminarRequisito([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        var r = await db.Requisitos.SingleOrDefaultAsync(r => r.Id == id && !r.Eliminado);
        if (r is null) return NotFound();
        if (r.Version != version) return Conflict("El requisito cambió. Recargue la página.");
        // Se retira del catálogo sin borrar el significado de los documentos históricos.
        r.Eliminado = true; r.Activo = false; r.Version++;
        return await GuardarCatalogo("Requisito eliminado del catálogo. El historial de trámites se conserva.");
    }

    [HttpGet]
    public async Task<IActionResult> Seccion(int? id)
    {
        if (!ModelState.IsValid) return BadRequest("Identificador inválido.");
        if (id is null) return View(new SeccionForm());
        var s = await db.SeccionesRequisitos.SingleOrDefaultAsync(s => s.Id == id && !s.Eliminada);
        return s is null ? NotFound() : View(new SeccionForm { Id = s.Id, Version = s.Version, Nombre = s.Nombre,
            Descripcion = s.Descripcion, Orden = s.Orden, Activa = s.Activa });
    }

    [HttpPost]
    public async Task<IActionResult> Seccion(SeccionForm model)
    {
        if (!ModelState.IsValid) return View(model);
        var s = model.Id == 0 ? new SeccionRequisito() : await db.SeccionesRequisitos.SingleOrDefaultAsync(s => s.Id == model.Id && !s.Eliminada);
        if (s is null) return NotFound();
        if (s.Id != 0 && s.Version != model.Version) return Conflict("La sección cambió. Recargue la página.");
        s.Nombre = model.Nombre.Trim(); s.Descripcion = model.Descripcion?.Trim() ?? ""; s.Orden = model.Orden; s.Activa = model.Activa;
        if (s.Id == 0) db.SeccionesRequisitos.Add(s); else s.Version++;
        return await GuardarCatalogo("Sección guardada.");
    }

    [HttpPost]
    public async Task<IActionResult> EstadoSeccion([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] bool activa)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        var s = await db.SeccionesRequisitos.SingleOrDefaultAsync(s => s.Id == id && !s.Eliminada);
        if (s is null) return NotFound();
        if (s.Version != version) return Conflict("La sección cambió. Recargue la página.");
        s.Activa = activa; s.Version++;
        return await GuardarCatalogo("Estado de la sección actualizado. Sus requisitos conservan su estado individual.");
    }

    [HttpPost]
    public async Task<IActionResult> EliminarSeccion([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        await using var transaction = await db.BeginTransactionAsync();
        var s = await db.SeccionesRequisitos.SingleOrDefaultAsync(s => s.Id == id && !s.Eliminada);
        if (s is null) return NotFound();
        if (s.Version != version) return Conflict("La sección cambió. Recargue la página.");
        if (await db.Requisitos.AnyAsync(r => r.SeccionId == id && !r.Eliminado))
        {
            TempData["Error"] = "Mueva o elimine los requisitos de esta sección antes de eliminarla.";
            return RedirectToAction(nameof(Requisitos));
        }
        s.Eliminada = true; s.Activa = false; s.Version++;
        var result = await GuardarCatalogo("Sección eliminada. Los expedientes enviados conservan su organización anterior.");
        if (result is RedirectToActionResult) await transaction.CommitAsync();
        return result;
    }

    private async Task<IActionResult> GuardarCatalogo(string mensaje)
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("El catálogo cambió. Recargue la página."); }
        TempData["Success"] = mensaje;
        return RedirectToAction(nameof(Requisitos));
    }
}

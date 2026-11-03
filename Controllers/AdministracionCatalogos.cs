using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;

public partial class AdministracionController
{
    [HttpGet]
    public async Task<IActionResult> Catalogos() => View(await db.Catalogos.AsNoTracking().OrderBy(c => c.Clase).ThenBy(c => c.Nombre).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Catalogo(int? id)
    {
        if (!ModelState.IsValid) return BadRequest("Identificador inválido.");
        if (id is null) return View(new CatalogoForm());
        var c = await db.Catalogos.FindAsync(id); if (c is null) return NotFound();
        return View(new CatalogoForm { Id = c.Id, Clase = c.Clase, Nombre = c.Nombre, Referencia = c.Referencia, Activo = c.Activo, Version = c.Version });
    }

    [HttpPost, RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Catalogo(CatalogoForm? model)
    {
        if (model is null) return BadRequest("No se pudo leer el formulario.");
        if (model.Clase == "feriado" && !DateTime.TryParseExact(model.Nombre, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            ModelState.AddModelError(nameof(model.Nombre), "Para feriados use una fecha válida en formato yyyy-MM-dd.");
        if (model.Clase is "tipo" or "municipio" && (model.Nombre?.Trim().Length ?? 0) > 100)
            ModelState.AddModelError(nameof(model.Nombre), "Máximo 100 caracteres.");
        if (model.Clase is "formulario_acta" or "formulario_citacion" && model.Activo &&
            await db.Catalogos.AnyAsync(c => c.Clase == model.Clase && c.Activo && c.Id != model.Id))
            ModelState.AddModelError("", "Desactive la plantilla anterior antes de activar una nueva.");
        if (await db.Catalogos.AnyAsync(c => c.Id != model.Id && c.Clase == model.Clase && c.Nombre == (model.Nombre ?? "").Trim()))
            ModelState.AddModelError(nameof(model.Nombre), "Ese elemento ya existe en el catálogo.");
        if (model.Plantilla is not null)
        {
            var error = await PdfStorage.ValidateAsync(model.Plantilla);
            if (error is not null) ModelState.AddModelError(nameof(model.Plantilla), error);
        }
        if (!ModelState.IsValid) return View(model);
        var c = model.Id == 0 ? new CatalogoItem() : await db.Catalogos.FindAsync(model.Id);
        if (c is null) return NotFound();
        if (c.Id != 0 && c.Version != model.Version) return Conflict("El catálogo cambió. Recargue la página.");
        if (c.Id != 0 && c.Clase != model.Clase) return BadRequest("No puede cambiar la clase de un registro existente.");
        if (model.Clase is "formulario_acta" or "formulario_citacion" && model.Plantilla is null && string.IsNullOrEmpty(c.ArchivoPdf))
        { ModelState.AddModelError(nameof(model.Plantilla), "Adjunte la plantilla institucional en PDF."); return View(model); }
        string? key = null;
        if (model.Plantilla is not null) { key = await storage.SaveAsync(model.Plantilla); c.ArchivoPdf = key; }
        c.Clase = model.Clase; c.Nombre = model.Nombre!.Trim(); c.Referencia = model.Referencia?.Trim() ?? ""; c.Activo = model.Activo;
        if (c.Id == 0) db.Catalogos.Add(c); else c.Version++;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { if (key is not null) storage.Delete(key); return Conflict("El catálogo cambió. Recargue la página."); }
        catch (DbUpdateException) { if (key is not null) storage.Delete(key); ModelState.AddModelError("", "No se pudo guardar. Compruebe si el elemento ya existe."); return View(model); }
        return RedirectToAction(nameof(Catalogos));
    }

    [HttpGet]
    public async Task<IActionResult> CalidadDatos()
    {
        ViewBag.SolicitudesInvalidas = (await db.Solicitudes.AsNoTracking().ToListAsync()).Where(s => {
            try { ExpedienteService.ValidarDatos(s); return false; } catch (InvalidOperationException) { return true; }
        }).Select(s => s.Id).ToList();
        var copias = await db.Solicitudes.AsNoTracking().Include(s => s.Inspecciones).ToListAsync();
        ViewBag.CopiasInvalidas = copias.Where(s => {
            try { ExpedienteService.LeerRequisitos(s); foreach(var i in s.Inspecciones) ExpedienteService.LeerVerificaciones(i); return false; }
            catch (InvalidOperationException) { return true; }
        }).Select(s => s.Id).ToList();
        ViewBag.SinCatalogo = await db.Catalogos.CountAsync(c => c.Activo && (c.Clase == "tipo" || c.Clase == "municipio"));
        ViewBag.ArchivosFaltantes = (await db.Documentos.AsNoTracking().ToListAsync()).Where(d => storage.Resolve(d.ArchivoPdf) is null).Select(d => d.Id).ToList();
        return View(await db.Laboratorios.AsNoTracking().Where(l => !l.DatosVerificados || l.PropietarioId == null || l.VigenciaHasta == null).OrderBy(l => l.Id).ToListAsync());
    }
}

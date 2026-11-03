using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;

public partial class GestionController
{
    [HttpGet, Authorize(Roles = "propietario")]
    public async Task<IActionResult> EditarDatos(int id)
    {
        var s = await db.Solicitudes.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id && s.PropietarioId == Actor);
        if (s is null) return NotFound();
        if (!DatosEditables(s)) return BadRequest("Solo puede corregir los datos de una apertura antes de su primer envío.");
        var model = new DatosBorradorForm { Id = s.Id, Version = s.Version, Municipio = s.Municipio,
            Direccion = s.Direccion, Telefono = s.Telefono, Codigo = s.Codigo };
        try { var datos = ExpedienteService.ValidarDatos(s); model.NombreLaboratorio = datos.Nombre;
            model.TipoLaboratorio = datos.Tipo; model.Latitud = datos.Lat; model.Longitud = datos.Lng; }
        catch (InvalidOperationException) { ModelState.AddModelError("", "Complete los datos del borrador para reparar el registro."); }
        await CatalogosDatos();
        return View(model);
    }

    [HttpPost, Authorize(Roles = "propietario")]
    public async Task<IActionResult> EditarDatos(DatosBorradorForm model)
    {
        var s = await db.Solicitudes.SingleOrDefaultAsync(s => s.Id == model.Id && s.PropietarioId == Actor);
        if (s is null) return NotFound();
        if (!DatosEditables(s)) return BadRequest("Los datos ya enviados no pueden alterarse desde este formulario.");
        if (s.Version != model.Version) return Conflict("El borrador cambió. Recargue antes de editarlo.");
        if (ModelState.IsValid)
        {
            var error = await ExpedienteService.ValidarCatalogos(db, model.TipoLaboratorio, model.Municipio, environment.IsDevelopment());
            if (error is not null) ModelState.AddModelError("", error);
        }
        if (!ModelState.IsValid) { Response.StatusCode = 400; await CatalogosDatos(); return View(model); }
        s.Municipio = model.Municipio.Trim(); s.Direccion = model.Direccion?.Trim() ?? "";
        s.Telefono = model.Telefono?.Trim() ?? ""; s.Codigo = model.Codigo?.Trim() ?? "";
        s.DatosLaboratorioPropuesto = JsonSerializer.Serialize(new { nombre = model.NombreLaboratorio.Trim(),
            tipo = model.TipoLaboratorio.Trim(), coordenadas = new { lat = model.Latitud, lng = model.Longitud } });
        tramites.Registrar(s, Actor, "Datos del borrador corregidos antes del primer envío.");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("El borrador cambió. Recargue antes de editarlo."); }
        TempData["Success"] = "Datos del borrador guardados.";
        return RedirectToAction(nameof(Detalle), new { id = s.Id });
    }

    private static bool DatosEditables(Solicitud s) => s.Estado == EstadoSolicitud.Borrador &&
        s.PrimerEnvio is null && s.FechaEnvio is null && !s.EsRenovacion && s.LaboratorioId is null;

    private async Task CatalogosDatos()
    {
        ViewBag.Tipos = await db.Catalogos.Where(c => c.Clase == "tipo" && c.Activo).Select(c => c.Nombre).OrderBy(n => n).ToListAsync();
        ViewBag.Municipios = await db.Catalogos.Where(c => c.Clase == "municipio" && c.Activo).Select(c => c.Nombre).OrderBy(n => n).ToListAsync();
    }
}

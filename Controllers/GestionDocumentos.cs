using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;

public partial class GestionController
{
    [HttpPost, Authorize(Roles = "supervisor"), RequestSizeLimit(11 * 1024 * 1024)]
    public Task<IActionResult> PrepararActa(DecisionModel? model)
    {
        if (model is null) return Task.FromResult<IActionResult>(BadRequest("No se pudo leer el formulario."));
        model.Accion = "preparar_acta";
        ModelState.Remove(nameof(DecisionModel.Accion));
        return Decidir(model);
    }


    private static byte[] CrearInforme(Solicitud s, IEnumerable<HistorialTramite> historial)
        => InformeTramite.Crear(s, historial);

    [HttpPost, Authorize(Roles = "coordinador")]
    public async Task<IActionResult> RevisarDocumento(RevisionDocumentoForm model)
    {
        if (model.Resultado == "aprobado" && string.IsNullOrWhiteSpace(model.Motivo))
        {
            model.Motivo = "Documento revisado y conforme.";
            ModelState.Remove(nameof(model.Motivo));
        }
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Motivo)) return BadRequest("Indique qué debe corregirse al observar un documento.");
        var s = await db.Solicitudes.Include(s => s.Documentos).SingleOrDefaultAsync(s => s.Id == model.Id && s.Estado == EstadoSolicitud.EnRevision);
        if (s is null) return NotFound();
        if (s.Version != model.Version) return Conflict("El trámite cambió. Recargue la página.");
        var d = s.Documentos.SingleOrDefault(d => d.Id == model.DocumentoId && d.Actual);
        if (d is null) return NotFound();
        if (storage.Resolve(d.ArchivoPdf) is null) return BadRequest("El archivo no está disponible. Solicite su reposición antes de aprobar.");
        if (d.EstadoRevision == "aprobado" && model.Resultado == "aprobado")
            return RedirectToAction(nameof(Detalle), new { id = s.Id });
        d.EstadoRevision = model.Resultado; d.Observacion = model.Motivo.Trim(); d.RevisorId = Actor; d.FechaRevision = DateTime.UtcNow;
        tramites.Registrar(s, Actor, $"Documento #{d.Id} ({d.NombreRequisito}, versión {d.VersionArchivo}): {model.Resultado}. {d.Observacion}");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("El trámite cambió. Recargue la página."); }
        return RedirectToAction(nameof(Detalle), new { id = s.Id });
    }

    [HttpPost, Authorize(Roles = "propietario")]
    public async Task<IActionResult> AsociarDocumento([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int documentoId, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int requisitoId, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        var s = await db.Solicitudes.Include(s => s.Documentos).SingleOrDefaultAsync(s => s.Id == id && s.PropietarioId == Actor);
        if (s is null) return NotFound();
        if (s.Version != version) return Conflict("El trámite cambió. Recargue la página.");
        if (!ExpedienteService.Editable(s)) return BadRequest("No se puede modificar documentación en esta etapa.");
        var documento = s.Documentos.SingleOrDefault(d => d.Id == documentoId && d.RequisitoId == 0 && d.Actual);
        var requisito = (await ExpedienteService.Requisitos(db, s)).SingleOrDefault(r => r.Id == requisitoId);
        if (documento is null || requisito is null) return BadRequest("Seleccione un documento complementario y un requisito del expediente.");
        if (storage.Resolve(documento.ArchivoPdf) is null) return BadRequest("El archivo no está disponible.");
        var anteriores = s.Documentos.Where(d => d.RequisitoId == requisitoId).ToList();
        if (anteriores.Any(d => d.Actual && d.EstadoRevision == "aprobado")) return BadRequest("El documento aprobado requiere una observación antes de reemplazarse.");
        foreach (var d in anteriores) d.Actual = false;
        documento.Actual = false;
        s.Documentos.Add(new Documento { RequisitoId = requisitoId, NombreRequisito = requisito.Nombre, ArchivoPdf = documento.ArchivoPdf, NombreArchivo = documento.NombreArchivo, VersionArchivo = anteriores.Count == 0 ? 1 : anteriores.Max(d => d.VersionArchivo) + 1 });
        tramites.Registrar(s, Actor, $"Documento complementario #{documento.Id} asociado a {requisito.Nombre}; se conserva el original.");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("El trámite cambió. Recargue la página."); }
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, Authorize(Roles = "supervisor"), RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Citacion(CitacionForm model)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los datos y adjunte la citación firmada en PDF o imagen.");
        await using var tx = await db.BeginTransactionAsync();
        var s = await Visibles().Include(s => s.Inspecciones).Include(s => s.Citaciones).SingleOrDefaultAsync(s => s.Id == model.Id);
        if (s is null) return NotFound();
        if (s.Version != model.Version) return Conflict("El trámite cambió. Recargue la página.");
        var i = s.Inspecciones.SingleOrDefault(i => i.Id == model.InspeccionId && i.SupervisorId == Actor);
        var ultima = s.Inspecciones.OrderByDescending(i => i.Id).FirstOrDefault();
        if (i is null || i != ultima || i.Estado is not ("registrada" or "validada") || i.Resultado is not ("observado" or "rechazado") || s.FechaCierre != null || s.Estado is EstadoSolicitud.Habilitado or EstadoSolicitud.RevisionLegal)
            return BadRequest("Solo puede citar por la última inspección propia con incumplimientos.");
        if (model.FechaEmision.Date < i.Fecha.Date || model.FechaEmision.Date > TramiteService.HoraBolivia.Date || model.Vencimiento.Date < model.FechaEmision.Date || model.Vencimiento.Date > TramiteService.HoraBolivia.Date.AddDays(365))
            return BadRequest("La fecha de emisión y el vencimiento deben corresponder a la inspección y no superar un año.");
        if (s.Citaciones.Any(c => c.InspeccionId == i.Id && c.FechaAtencion == null)) return Conflict("Esta inspección ya tiene una citación pendiente.");
        var error = await EvidenciaArchivo.Validar(model.Evidencia!);
        if (error is not null) return BadRequest(error);
        var key = await EvidenciaArchivo.Guardar(model.Evidencia!, storage);
        var c = new Citacion { Solicitud = s, InspeccionId = i.Id, SupervisorId = Actor, FechaEmision = model.FechaEmision.Date, Vencimiento = model.Vencimiento.Date, AnticipacionDias = model.AnticipacionDias, Motivo = model.Motivo.Trim(), EvidenciaPdf = key };
        db.Citaciones.Add(c);
        tramites.Registrar(s, Actor, $"Citación por inspección #{i.Id}, vencimiento {c.Vencimiento:dd/MM/yyyy}. {c.Motivo}");
        try { await db.SaveChangesAsync(); await tx.CommitAsync(); }
        catch (DbUpdateConcurrencyException) { storage.Delete(key); return Conflict("El trámite cambió. Recargue la página."); }
        catch { storage.Delete(key); throw; }
        return RedirectToAction(nameof(Detalle), new { id = s.Id });
    }

    [HttpGet, Authorize(Roles = "supervisor,coordinador")]
    public async Task<IActionResult> Citaciones()
    {
        var ids = Visibles().Select(s => s.Id);
        return View(await db.Citaciones.AsNoTracking().Include(c => c.Solicitud)
            .Where(c => ids.Contains(c.SolicitudId) && (!User.IsInRole("supervisor") || c.SupervisorId == Actor))
            .OrderByDescending(c => c.FechaEmision).ThenByDescending(c => c.Id).ToListAsync());
    }

    [HttpGet, Authorize(Roles = "supervisor,coordinador")]
    public async Task<IActionResult> PlantillaCitacion()
    {
        var oficial = await db.Catalogos.AsNoTracking().Where(c => c.Activo && c.Clase == "formulario_citacion").OrderByDescending(c => c.Id).FirstOrDefaultAsync();
        var path = oficial is null ? null : storage.Resolve(oficial.ArchivoPdf);
        return path is null ? NotFound("La institución todavía no cargó la plantilla de citación.") : PhysicalFile(path, "application/pdf", $"plantilla-citacion-v{oficial!.Version}.pdf");
    }

    [HttpPost, Authorize(Roles = "propietario")]
    public Task<IActionResult> RetirarComplementario([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int documentoId, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version)
        => RetirarArchivo(id, documentoId, version, soloComplemento: true);

    [HttpPost, Authorize(Roles = "propietario")]
    public Task<IActionResult> RetirarDocumento([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int documentoId, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version)
        => RetirarArchivo(id, documentoId, version, soloComplemento: false);

    private async Task<IActionResult> RetirarArchivo(int id, int documentoId, int version, bool soloComplemento)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        var s = await db.Solicitudes.Include(s => s.Documentos).SingleOrDefaultAsync(s => s.Id == id && s.PropietarioId == Actor);
        if (s is null) return NotFound();
        if (s.Version != version) return Conflict("El trámite cambió. Recargue la página.");
        if (!ExpedienteService.Editable(s)) return BadRequest("No se puede modificar documentación en esta etapa.");
        var d = s.Documentos.SingleOrDefault(d => d.Id == documentoId && d.Actual && (!soloComplemento || d.RequisitoId == 0) && d.EstadoRevision is "pendiente" or "observado");
        if (d is null) return BadRequest("Solo puede quitar un documento propio pendiente u observado de un borrador o solicitud devuelta para corregir.");
        d.Actual = false;
        tramites.Registrar(s, Actor, $"Documento #{d.Id} retirado de la presentación actual. Se conserva el archivo y la revisión anterior.");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Conflict("El trámite cambió. Recargue la página."); }
        TempData["Success"] = "PDF quitado de la lista de envío. Puede agregar otro cuando lo necesite.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpGet, Authorize(Roles = "propietario")]
    public async Task<IActionResult> Establecimiento(int id)
    {
        var lab = await db.Laboratorios.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id && l.PropietarioId == Actor);
        if (lab is null) return NotFound();
        ViewBag.Laboratorio = lab;
        return View(await db.Solicitudes.AsNoTracking().Where(s => s.LaboratorioId == id && s.PropietarioId == Actor).OrderByDescending(s => s.FechaCreacion).ToListAsync());
    }
}

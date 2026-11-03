using SEDESLABORATORIO.Application.Tramites;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;

[Authorize(Roles = "propietario,coordinador,supervisor,abogado")]
[AutoValidateAntiforgeryToken]
public partial class GestionController(IGestionDatos db, IRegistroTramite tramites, IAlmacenPdf storage, IWebHostEnvironment environment, IConfiguration configuration, IDecisionesTramite decisiones, IVisibilidadTramites visibilidad, IConsultaAgenda agendaSupervisor) : Controller
{
    private int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<Solicitud> Visibles() => visibilidad.Visibles(Actor, User.FindFirstValue(ClaimTypes.Role) ?? "");

    [HttpGet]
    public async Task<IActionResult> Index(EstadoSolicitud? estado, [System.ComponentModel.DataAnnotations.StringLength(100), TextoSeguro] string? municipio)
    {
        var query = Visibles().AsNoTracking();
        if (!ModelState.IsValid) { Response.StatusCode = 400; TempData["Error"] = "Revise el estado y el municipio del filtro. Corrija los valores y vuelva a filtrar."; query = query.Where(s => false); }
        if (estado.HasValue) query = query.Where(s => s.Estado == estado);
        if (!string.IsNullOrWhiteSpace(municipio)) query = query.Where(s => s.Municipio == municipio.Trim());
        ViewBag.Estado = estado; ViewBag.Municipio = municipio;
        return View(await query.Include(s => s.Propietario).OrderByDescending(s => s.FechaCreacion).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var solicitud = await Visibles().AsNoTracking().AsSplitQuery().Include(s => s.Propietario).Include(s => s.Documentos)
            .Include(s => s.Inspecciones).ThenInclude(i => i.Supervisor).Include(s => s.Historial).ThenInclude(h => h.Actor)
            .Include(s => s.Citaciones).Include(s => s.Laboratorio).SingleOrDefaultAsync(s => s.Id == id);
        if (solicitud is null) return NotFound();
        List<RequisitoCopia> requisitos;
        try { requisitos = await ExpedienteService.Requisitos(db, solicitud); foreach(var i in solicitud.Inspecciones) ExpedienteService.LeerVerificaciones(i); }
        catch (InvalidOperationException ex) { Response.StatusCode = 409; ViewBag.Problema = ex.Message; ViewBag.SolicitudId = id; return View("DatosInconsistentes"); }
        return View(User.IsInRole("supervisor") ? "Inspeccion" : "Detalle", new TramiteDetalleModel { Solicitud = solicitud,
            Requisitos = requisitos,
            Supervisores = User.IsInRole("coordinador") ? await db.Usuarios.Where(u => u.Activo && u.Rol == RolUsuario.Supervisor).ToListAsync() : [] });
    }

    [HttpPost, Authorize(Roles = "propietario"), RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Documento([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int requisitoId, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int version, IFormFile? pdf)
    {
        if (!ModelState.IsValid) return BadRequest("No se pudo leer el formulario de carga.");
        var s = await db.Solicitudes.Include(x => x.Documentos).SingleOrDefaultAsync(x => x.Id == id && x.PropietarioId == Actor);
        if (s is null) return NotFound();
        if (s.Version != version) return Conflict("El trámite cambió. Recargue la página.");
        if (s.Estado is not (EstadoSolicitud.Borrador or EstadoSolicitud.Observado)) return BadRequest("No se pueden modificar documentos en esta etapa.");
        var requisito = requisitoId == 0 ? new RequisitoCopia(0, "Documento complementario", false, 1, "") : (await ExpedienteService.Requisitos(db, s)).SingleOrDefault(r => r.Id == requisitoId);
        if (requisito is null) return BadRequest("Requisito inválido para este expediente.");
        if (requisitoId != 0 && s.Documentos.Any(d => d.Actual && d.RequisitoId == requisitoId && d.EstadoRevision == "aprobado"))
            return BadRequest("Un documento aprobado solo puede reemplazarse después de una observación del revisor.");
        var error = pdf is null ? "Seleccione un PDF." : await PdfStorage.ValidateAsync(pdf);
        if (error is not null) { TempData["Error"] = error; return RedirectToAction(nameof(Detalle), new { id }); }
        var key = await storage.SaveAsync(pdf!);
        var previous = s.Documentos.Where(d => requisitoId != 0 && d.RequisitoId == requisitoId).ToList();
        foreach (var d in previous) d.Actual = false;
        var archivoVersion = previous.Count == 0 ? 1 : previous.Max(d => d.VersionArchivo) + 1;
        s.Documentos.Add(new Documento { RequisitoId = requisitoId, NombreRequisito = requisito.Nombre, ArchivoPdf = key, NombreArchivo = PdfStorage.DisplayName(pdf!), VersionArchivo = archivoVersion });
        tramites.Registrar(s, Actor, $"Documento {requisito.Nombre}, versión {archivoVersion}, cargado para revisión.");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { storage.Delete(key); return Conflict("El trámite cambió. Recargue la página."); }
        catch { storage.Delete(key); throw; }
        // Se conservan archivos reemplazados para respaldo; nunca se sirven sin registro autorizado.
        TempData["Success"] = "Documento guardado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Descargar(int id, string tipo = "documento")
    {
        string? key; int solicitudId;
        if (tipo == "documento")
        {
            var doc = await db.Documentos.FindAsync(id); if (doc is null) return NotFound();
            key = doc.ArchivoPdf; solicitudId = doc.SolicitudId;
        }
        else if (tipo == "acta")
        {
            var acta = await db.Inspecciones.FindAsync(id); if (acta is null) return NotFound();
            key = acta.ActaPdf; solicitudId = acta.SolicitudId;
        }
        else if (tipo == "legal")
        {
            var s = await db.Solicitudes.FindAsync(id); if (s is null) return NotFound();
            key = s.FirmaLegalPdf; solicitudId = s.Id;
        }
        else if (tipo == "citacion")
        {
            var c = await db.Citaciones.FindAsync(id); if (c is null) return NotFound();
            key = c.EvidenciaPdf; solicitudId = c.SolicitudId;
        }
        else return NotFound();
        if (!await Visibles().AnyAsync(s => s.Id == solicitudId)) return NotFound();
        var path = storage.Resolve(key);
        return path is null ? NotFound() : PhysicalFile(path, EvidenciaArchivo.Tipo(key!), $"{tipo}-{id}{Path.GetExtension(key)}");
    }

    [HttpPost, RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Decidir(DecisionModel? model)
    {
        if (model is null) return BadRequest("No se pudo leer el formulario.");
        if (!ModelState.IsValid) {
            if (model.Accion is "acta" or "preparar_acta") return await MostrarErrorActa(model, "Revise las verificaciones, los textos y el plazo; los datos ingresados se conservan. Vuelva a seleccionar el PDF.");
            TempData["Error"] = "Revise los campos: texto demasiado largo, fecha o plazo inválido."; return RedirectToAction(nameof(Detalle), new { model.Id });
        }
        var resultado = await decisiones.Ejecutar(new(Actor, User.FindFirstValue(ClaimTypes.Role) ?? ""), model);
        if (resultado.Estado == EstadoDecision.Inexistente) return NotFound();
        if (resultado.Estado == EstadoDecision.Conflicto) return Conflict(resultado.Mensaje);
        if (resultado.Estado == EstadoDecision.Invalida) return BadRequest(resultado.Mensaje);
        if (resultado.Estado == EstadoDecision.NoPermitida)
        {
            if (model.Accion is "acta" or "preparar_acta") return await MostrarErrorActa(model, resultado.Mensaje + " Los datos ingresados se conservan; vuelva a seleccionar el PDF.");
            TempData["Error"] = resultado.Mensaje;
        }
        else TempData["Success"] = resultado.Mensaje;
        return RedirectToAction(nameof(Detalle), new { model.Id });
    }

    private async Task<IActionResult> MostrarErrorActa(DecisionModel model, string mensaje)
    {
        ViewBag.ActaIntento = model;
        TempData["Error"] = mensaje;
        Response.StatusCode = 400;
        return await Detalle(model.Id);
    }

    [HttpGet, Authorize(Roles = "supervisor")]
    public async Task<IActionResult> Agenda(DateTime? fecha)
    {
        if (!ModelState.IsValid) return BadRequest("Fecha inválida.");
        return View(await CargarAgenda(fecha, false));
    }

    [HttpGet, Authorize(Roles = "supervisor,coordinador")]
    public async Task<IActionResult> FormularioActa(int id, bool resumen = false)
    {
        var s = await Visibles().Include(s => s.Inspecciones).ThenInclude(i => i.Supervisor).SingleOrDefaultAsync(s => s.Id == id); if (s is null) return NotFound();
        var data = TramiteDetalleModel.DatosDe(s);
        var i = s.Inspecciones.Where(i => !User.IsInRole("supervisor") || i.SupervisorId == Actor).OrderByDescending(i => i.Id).FirstOrDefault();
        if (i is null) return BadRequest("Debe asignar una inspección antes de descargar el acta.");
        var plantilla = storage.Resolve(i.PlantillaPdf);
        if (plantilla is not null && !resumen) return PhysicalFile(plantilla, "application/pdf", $"plantilla-acta-v{i.PlantillaVersion}.pdf");
        var lines = new List<string> { "SEDES - ACTA DE INSPECCIÓN", $"Solicitud #{s.Id} - {data.NombreLaboratorio}", $"Municipio: {s.Municipio} - Dirección: {s.Direccion}", $"Ubicación: {data.Coordenadas}", $"Visita: {(i.Programada ? i.Fecha.ToString("dd/MM/yyyy HH:mm") : "Por programar")} (Bolivia)", $"Supervisor: {i.Supervisor.Nombre}", "Lista de verificación:" };
        List<Verificacion> verificacion;
        try { verificacion = ExpedienteService.LeerVerificaciones(i); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        if (verificacion.Count == 0) lines.Add("Catálogo institucional de verificaciones pendiente de configuración.");
        lines.AddRange(verificacion.SelectMany(v => new[] { $"{v.Nombre} - {v.Referencia} (versión {v.Version})", $"Cumple / Observado / No aplica: {v.Resultado}", $"Observaciones: {v.Observacion}", "___________________________________________________" }));
        lines.AddRange([$"Resultado: {i.Resultado}", $"Hallazgos: {i.Acta}", $"Plazo de subsanación: {i.PlazoSubsanacion:dd/MM/yyyy}", "Firma supervisor: ______________ Firma responsable: ______________"]);
        return File(SimplePdf.Create(lines), "application/pdf", $"formulario-acta-{id}.pdf");
    }

    [HttpGet, Authorize(Roles = "coordinador")]
    public async Task<IActionResult> Informe(int id)
    {
        var s = await db.Solicitudes.Include(x => x.Historial).ThenInclude(h => h.Actor).Include(x => x.Laboratorio).SingleOrDefaultAsync(s => s.Id == id);
        if (s is null) return NotFound();
        if (s.Estado != EstadoSolicitud.Habilitado) return BadRequest("El trámite todavía no está cerrado.");
        return File(s.InformeCierrePdf ?? CrearInforme(s, s.Historial), "application/pdf", $"cierre-{id}.pdf");
    }

    [HttpGet, Authorize(Roles = "propietario")]
    public async Task<IActionResult> Establecimientos() => View(await db.Laboratorios.AsNoTracking().Where(l => l.PropietarioId == Actor).ToListAsync());

    [HttpPost, Authorize(Roles = "propietario")]
    public async Task<IActionResult> Servicios([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id, [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] string version, [TextoSeguro(Multilinea = true)] string? servicios)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        var lab = await db.Laboratorios.SingleOrDefaultAsync(l => l.Id == id && l.PropietarioId == Actor);
        if (lab is null) return NotFound();
        if ((servicios?.Length ?? 0) > 2000) return BadRequest("Máximo 2000 caracteres.");
        if (version != RevisionServicios.De(lab.Servicios)) return Conflict("Los servicios cambiaron. Recargue la página antes de guardar.");
        var texto = servicios?.Trim() ?? "";
        var updated = await db.Laboratorios.Where(l => l.Id == id && l.PropietarioId == Actor && l.Servicios == lab.Servicios)
            .ExecuteUpdateAsync(set => set.SetProperty(l => l.Servicios, texto));
        if (updated != 1) return Conflict("Los servicios cambiaron. Recargue la página antes de guardar.");
        return RedirectToAction(nameof(Establecimientos));
    }

    [HttpPost, Authorize(Roles = "propietario")]
    public async Task<IActionResult> Renovar([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        await using var transaction = await db.BeginTransactionAsync();
        var lab = await db.Laboratorios.SingleOrDefaultAsync(l => l.Id == id && l.PropietarioId == Actor);
        if (lab is null) return NotFound();
        if (!lab.DatosVerificados) { TempData["Error"] = "El establecimiento requiere validación institucional antes de renovar."; return RedirectToAction(nameof(Establecimientos)); }
        var ventana = configuration.GetValue<int?>("Tramites:RenovacionDiasAnticipacion");
        if (ventana.HasValue && lab.VigenciaHasta > DateTime.UtcNow.AddDays(Math.Clamp(ventana.Value, 0, 365)))
        { TempData["Error"] = $"La renovación se admite dentro de los {Math.Clamp(ventana.Value, 0, 365)} días previos al vencimiento."; return RedirectToAction(nameof(Establecimientos)); }
        if (await db.Solicitudes.AnyAsync(s => s.LaboratorioId == id && s.Estado != EstadoSolicitud.Habilitado && (s.Estado != EstadoSolicitud.Rechazado || s.MotivoRechazo == "campo")))
        { TempData["Error"] = "Ya existe una renovación pendiente."; return RedirectToAction(nameof(Establecimientos)); }
        var s = new Solicitud { PropietarioId = Actor, LaboratorioId = lab.Id, EsRenovacion = true, Municipio = lab.Municipio, Estado = EstadoSolicitud.Borrador, FechaCreacion = DateTime.UtcNow,
            Direccion = lab.Direccion, Telefono = lab.Telefono, Codigo = lab.Codigo,
            DatosLaboratorioPropuesto = JsonSerializer.Serialize(new { nombre = lab.Nombre, tipo = lab.Tipo, coordenadas = new { lat = lab.Lat, lng = lab.Lng } }) };
        db.Solicitudes.Add(s); await db.SaveChangesAsync(); await transaction.CommitAsync();
        return RedirectToAction(nameof(Detalle), new { s.Id });
    }
}

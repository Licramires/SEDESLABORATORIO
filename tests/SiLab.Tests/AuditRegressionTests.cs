using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class WorkflowTests
{
    private async Task<int> Ready(HttpClient owner, HttpClient coordinator, string name)
    {
        var id = await Draft(owner, name); await UploadRequirements(owner, id); await Decide(owner, id, "enviar");
        await ReviewDocuments(coordinator, id); await Decide(coordinator, id, "aprobar_documentos");
        Assert.Equal(EstadoSolicitud.Aprobado, await State(id)); return id;
    }

    private async Task<int> Legal(HttpClient owner, HttpClient coordinator, HttpClient supervisor, string name)
    {
        var id = await Ready(owner, coordinator, name);
        var supervisorId = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString() });
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id); i.Programada = true; i.Fecha = TramiteService.HoraBolivia.AddMinutes(-10); i.FechaFin = TramiteService.HoraBolivia.AddMinutes(50); return await db.SaveChangesAsync(); });
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "aprobado" }, Pdf);
        await Decide(coordinator, id, "aprobar_acta"); Assert.Equal(EstadoSolicitud.RevisionLegal, await State(id)); return id;
    }

    [Fact]
    public async Task Optional_catalogue_can_finish_document_review_without_pdf()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador");
        var flags = await app.Db(db => db.Requisitos.ToDictionaryAsync(r => r.Id, r => r.Obligatorio));
        try
        {
            await app.Db(async db => { foreach(var r in await db.Requisitos.ToListAsync()) r.Obligatorio = false; return await db.SaveChangesAsync(); });
            var id = await Draft(owner, "Sin documentos obligatorios"); await Decide(owner, id, "enviar");
            Assert.Equal(EstadoSolicitud.EnRevision, await State(id));
            await Decide(coordinator, id, "aprobar_documentos"); Assert.Equal(EstadoSolicitud.Aprobado, await State(id));
        }
        finally { await app.Db(async db => { foreach(var r in await db.Requisitos.ToListAsync()) if(flags.TryGetValue(r.Id,out var value)) r.Obligatorio = value; return await db.SaveChangesAsync(); }); }
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"nombre\":\"Incompleto\",\"tipo\":\"Clínico\"}")]
    [InlineData("{\"nombre\":\"Fuera de rango\",\"tipo\":\"Clínico\",\"coordenadas\":{\"lat\":100,\"lng\":0}}")]
    public async Task Legal_approval_rejects_damaged_official_data_without_side_effects(string json)
    {
        using var lawyer = await Login("abogado");
        var id = await app.Db(async db => { var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario); var s = new Solicitud { PropietarioId = owner.Id, Estado = EstadoSolicitud.RevisionLegal, DatosLaboratorioPropuesto = json, Municipio = "Sacaba", FechaCreacion = DateTime.UtcNow }; db.Add(s); await db.SaveChangesAsync(); return s.Id; });
        var before = await app.Db(db => db.Laboratorios.CountAsync());
        await Decide(lawyer, id, "aprobar_legal", file: Pdf);
        Assert.Equal(EstadoSolicitud.RevisionLegal, await State(id)); Assert.Equal(before, await app.Db(db => db.Laboratorios.CountAsync()));
        Assert.Equal("", await app.Db(db => db.Solicitudes.Where(s => s.Id == id).Select(s => s.FirmaLegalPdf).SingleAsync()));
    }

    [Fact]
    public async Task Legal_observation_cannot_skip_review_and_does_not_repeat_field_visit_unnecessarily()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador"); using var supervisor = await Login("supervisor"); using var lawyer = await Login("abogado");
        var id = await Legal(owner, coordinator, supervisor, "Subsanación legal");
        var legalDocs = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id && d.Actual).Select(d => d.Id).ToListAsync());
        await Decide(lawyer, id, "observar_legal", legalDocs.Select((d,n) => new { Key = $"DocumentosObservados[{n}]", Value = d.ToString() }).ToDictionary(x => x.Key, x => x.Value)); Assert.Equal(EstadoSolicitud.Observado, await State(id));
        var supervisorId = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString() });
        Assert.Equal(EstadoSolicitud.Observado, await State(id));
        await Decide(owner, id, "enviar"); Assert.Equal(EstadoSolicitud.Observado, await State(id));
        await UploadRequirements(owner, id); await Decide(owner, id, "enviar"); await ReviewDocuments(coordinator, id); await Decide(coordinator, id, "aprobar_documentos");
        Assert.Equal(EstadoSolicitud.RevisionLegal, await State(id)); Assert.Equal(1, await app.Db(db => db.Inspecciones.CountAsync(i => i.SolicitudId == id)));
        await Decide(lawyer, id, "rechazar_legal"); Assert.Equal(EstadoSolicitud.Rechazado, await State(id));
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString() }); Assert.Equal(EstadoSolicitud.Rechazado, await State(id));
    }

    [Fact]
    public async Task Document_versions_preserve_original_and_requirement_snapshot()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador");
        var id = await Draft(owner, "Versiones de documentos"); await UploadRequirements(owner, id); await Decide(owner, id, "enviar");
        var original = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id).FirstAsync());
        var req = await app.Db(db => db.Requisitos.SingleAsync(r => r.Id == original.RequisitoId));
        try
        {
            await Post(coordinator, $"/Gestion/Detalle/{id}", "/Gestion/RevisarDocumento", new() { ["Id"] = id.ToString(), ["DocumentoId"] = original.Id.ToString(), ["Version"] = (await Version(id)).ToString(), ["Resultado"] = "observado", ["Motivo"] = "Firma pendiente" });
            await Decide(coordinator, id, "observar_documentos"); Assert.Equal(EstadoSolicitud.Observado, await State(id));
            await app.Db(async db => { var r = await db.Requisitos.FindAsync(req.Id); r!.Nombre = "Catálogo editado después del envío"; r.Activo = false; r.Version++; return await db.SaveChangesAsync(); });
            var html = await owner.GetStringAsync($"/Gestion/Detalle/{id}"); Assert.Contains(WebUtility.HtmlEncode(req.Nombre), html); Assert.DoesNotContain("Catálogo editado después del envío", WebUtility.HtmlDecode(html));
            await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/Documento", new() { ["id"] = id.ToString(), ["requisitoId"] = req.Id.ToString(), ["version"] = (await Version(id)).ToString() }, Pdf, "pdf");
            var versions = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id && d.RequisitoId == req.Id).OrderBy(d => d.Id).ToListAsync());
            Assert.Equal(2, versions.Count); Assert.False(versions[0].Actual); Assert.Equal("observado", versions[0].EstadoRevision); Assert.True(versions[1].Actual); Assert.Equal(2, versions[1].VersionArchivo);
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/Gestion/Descargar/{original.Id}")).StatusCode);
            await Decide(owner, id, "enviar"); Assert.Equal(EstadoSolicitud.EnRevision, await State(id));
        }
        finally { await app.Db(async db => { var r = await db.Requisitos.FindAsync(req.Id); r!.Nombre = req.Nombre; r.Activo = req.Activo; r.Version++; return await db.SaveChangesAsync(); }); }
    }

    [Fact]
    public async Task Initial_upload_can_be_associated_without_reuploading()
    {
        using var owner = await Login("propietario");
        var response = await Post(owner, "/solicitud-registro/SolicitudRegistro/Nueva", "/solicitud-registro/SolicitudRegistro/Nueva", new() { ["NombreLaboratorio"] = "Carga asociada", ["TipoLaboratorio"] = "Clínico", ["Municipio"] = "Sacaba", ["Latitud"] = "-17.39", ["Longitud"] = "-66.15" }, Pdf, "DocumentosPdf");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var id = int.Parse(System.Text.RegularExpressions.Regex.Match(response.Headers.Location!.ToString(), @"\d+$").Value);
        var doc = await app.Db(db => db.Documentos.SingleAsync(d => d.SolicitudId == id)); var req = await app.Db(db => db.Requisitos.Where(r => r.Activo).Select(r => r.Id).FirstAsync());
        var associated = await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/AsociarDocumento", new() { ["id"] = id.ToString(), ["documentoId"] = doc.Id.ToString(), ["requisitoId"] = req.ToString(), ["version"] = (await Version(id)).ToString() });
        Assert.Equal(HttpStatusCode.Redirect, associated.StatusCode);
        Assert.True(await app.Db(db => db.Documentos.AnyAsync(d => d.SolicitudId == id && d.RequisitoId == req && d.Actual && d.ArchivoPdf == doc.ArchivoPdf)));
        Assert.False(await app.Db(db => db.Documentos.Where(d => d.Id == doc.Id).Select(d => d.Actual).SingleAsync()));
    }

    [Fact]
    public async Task Supervisor_can_program_intervals_without_colliding_with_own_reservation()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador"); using var supervisor = await Login("supervisor");
        var id = await Ready(owner, coordinator, "Intervalos del supervisor");
        var sup = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = sup.ToString() });
        Assert.False(await app.Db(db => db.Inspecciones.Where(i => i.SolicitudId == id).Select(i => i.Programada).SingleAsync()));
        var date = TramiteService.HoraBolivia.AddDays(25).Date.AddHours(9);
        var fields = new Dictionary<string,string> { ["Fecha"] = date.ToString("s"), ["FechaFin"] = date.AddHours(2).ToString("s") };
        await Decide(supervisor, id, "programar", fields);
        var version = await Version(id); await Decide(supervisor, id, "programar", new() { ["Fecha"] = date.AddMinutes(30).ToString("s"), ["FechaFin"] = date.AddHours(2).ToString("s") });
        Assert.True(await Version(id) > version);
        var other = await Ready(owner, coordinator, "Conflicto real de intervalos");
        await Decide(coordinator, other, "asignar", new() { ["SupervisorId"] = sup.ToString(), ["Fecha"] = date.AddHours(1).ToString("s"), ["FechaFin"] = date.AddHours(3).ToString("s") });
        Assert.Equal(EstadoSolicitud.Aprobado, await State(other));
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "aprobado" }, Pdf); Assert.Equal(EstadoSolicitud.Inspeccion, await State(id));
    }

    [Fact]
    public async Task Historical_closure_pdf_stays_identical_after_laboratory_changes()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador"); using var supervisor = await Login("supervisor"); using var lawyer = await Login("abogado");
        var id = await Legal(owner, coordinator, supervisor, "Cierre inmutable"); await Decide(lawyer, id, "aprobar_legal", file: Pdf);
        Assert.Equal(EstadoSolicitud.Habilitado, await State(id)); var before = await coordinator.GetByteArrayAsync($"/Gestion/Informe/{id}");
        await app.Db(async db => { var s = await db.Solicitudes.Include(s => s.Laboratorio).SingleAsync(s => s.Id == id); s.Laboratorio!.VigenciaHasta = s.Laboratorio.VigenciaHasta!.Value.AddYears(1); s.Laboratorio.Nombre = "Nombre posterior"; return await db.SaveChangesAsync(); });
        var after = await coordinator.GetByteArrayAsync($"/Gestion/Informe/{id}"); Assert.Equal(before, after);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Citations_require_evidence_and_notify_both_roles_once_per_phase(bool imagen)
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador"); using var supervisor = await Login("supervisor");
        var id = await Ready(owner, coordinator, "Citación con evidencia"); var sup = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = sup.ToString() });
        var inspection = await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id); i.Programada = true; i.Fecha = TramiteService.HoraBolivia.AddMinutes(-5); await db.SaveChangesAsync(); return i.Id; });
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "observado" }, Pdf); await Decide(coordinator, id, "aprobar_acta");
        var fields = new Dictionary<string,string> { ["Id"] = id.ToString(), ["InspeccionId"] = inspection.ToString(), ["Version"] = (await Version(id)).ToString(), ["Motivo"] = "Subsanar incumplimiento", ["FechaEmision"] = TramiteService.HoraBolivia.ToString("yyyy-MM-dd"), ["Vencimiento"] = TramiteService.HoraBolivia.AddDays(2).ToString("yyyy-MM-dd"), ["AnticipacionDias"] = "5" };
        var invalid = await Post(supervisor, $"/Gestion/Detalle/{id}", "/Gestion/Citacion", new(fields)); Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a4x8AAAAASUVORK5CYII=");
        var created = await Post(supervisor, $"/Gestion/Detalle/{id}", "/Gestion/Citacion", fields, imagen ? imageBytes : Pdf, "Evidencia", imagen ? "citacion.png" : "citacion.pdf"); Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var cId = await app.Db(db => db.Citaciones.Where(c => c.SolicitudId == id).Select(c => c.Id).SingleAsync());
        for(var n=0;n<2;n++) await app.Db(async db => { await new TramiteService(db).AlertasVigencia(); return 0; });
        Assert.Equal(2, await app.Db(db => db.Notificaciones.CountAsync(n => n.Clave != null && n.Clave.StartsWith($"citacion:{cId}:"))));
        await app.Db(async db => { var c = await db.Citaciones.FindAsync(cId); c!.Vencimiento = TramiteService.HoraBolivia.AddDays(-1).Date; var i = await db.Inspecciones.FindAsync(inspection); i!.PlazoSubsanacion = TramiteService.HoraBolivia.AddDays(-1).Date; return await db.SaveChangesAsync(); });
        for(var n=0;n<2;n++) await app.Db(async db => { await new TramiteService(db).AlertasVigencia(); return 0; });
        Assert.Equal(2, await app.Db(db => db.Notificaciones.CountAsync(n => n.Clave != null && n.Clave.StartsWith($"subsanacion:{inspection}:"))));
        var evidence = await owner.GetAsync($"/Gestion/Descargar/{cId}?tipo=citacion");Assert.Equal(HttpStatusCode.OK, evidence.StatusCode);Assert.Equal(imagen ? "image/png" : "application/pdf", evidence.Content.Headers.ContentType!.MediaType);
        using var manager = await Login("gerente"); var denied = await manager.GetAsync($"/Gestion/Descargar/{cId}?tipo=citacion"); Assert.Contains("AccessDenied", denied.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Manager_excludes_drafts_and_export_uses_same_scope()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador"); using var manager = await Login("gerente");
        var draft = await Draft(owner, "Borrador privado de métricas"); var sent = await Ready(owner, coordinator, "Trámite recibido de métricas");
        var municipality = "Municipio métrica " + Guid.NewGuid().ToString("N")[..8];
        await app.Db(async db => { foreach(var s in await db.Solicitudes.Where(s => s.Id == draft || s.Id == sent).ToListAsync()) s.Municipio = municipality; return await db.SaveChangesAsync(); });
        var response = await manager.GetStringAsync("/Metricas?municipio=" + Uri.EscapeDataString(municipality)); Assert.Contains("1", response);
        var csv = await manager.GetStringAsync("/Metricas?descargar=true&municipio=" + Uri.EscapeDataString(municipality));
        Assert.Contains($"\"{municipality}\",1,0,0,1,1,0", csv); Assert.Contains("BorradoresCreadosEnPeriodo,1", csv); Assert.DoesNotContain("Borrador privado de métricas", csv);
    }

    [Fact]
    public async Task Public_templates_are_separate_from_private_documents_and_catalogues_validate_values()
    {
        using var admin = await Login("administrador"); using var anonymous = app.Browser(); using var owner = await Login("propietario");
        var req = await Post(admin, "/Administracion/Requisito", "/Administracion/Requisito", new() { ["Nombre"] = "Plantilla pública de prueba", ["Obligatorio"] = "false", ["Activo"] = "true" }, Pdf, "Plantilla"); Assert.Equal(HttpStatusCode.Redirect, req.StatusCode);
        var reqId = await app.Db(db => db.Requisitos.Where(r => r.Nombre == "Plantilla pública de prueba").Select(r => r.Id).SingleAsync());
        var template = await anonymous.GetAsync($"/laboratorios/Plantilla/{reqId}"); Assert.Equal(HttpStatusCode.OK, template.StatusCode); Assert.Equal("application/pdf", template.Content.Headers.ContentType!.MediaType);
        var html = WebUtility.HtmlDecode(await anonymous.GetStringAsync("/laboratorios")); Assert.Contains("Plantilla pública de prueba", html);
        var typeId = await app.Db(async db => { var c = new CatalogoItem { Clase = "tipo", Nombre = "Tipo permitido de prueba" }; db.Add(c); await db.SaveChangesAsync(); return c.Id; });
        try {
            var invalid = await Post(owner, "/solicitud-registro/SolicitudRegistro/Nueva", "/solicitud-registro/SolicitudRegistro/Nueva", new() { ["NombreLaboratorio"] = "Tipo no autorizado", ["TipoLaboratorio"] = "No existe", ["Municipio"] = "Sacaba", ["Latitud"] = "-17.39", ["Longitud"] = "-66.15" }); Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
            Assert.False(await app.Db(db => db.Solicitudes.AnyAsync(s => s.DatosLaboratorioPropuesto.Contains("Tipo no autorizado"))));
        } finally { await app.Db(async db => { db.Catalogos.Remove((await db.Catalogos.FindAsync(typeId))!); return await db.SaveChangesAsync(); }); }
        var labId = await app.Db(async db => {
            var lab = new Laboratorio { Nombre = "Laboratorio para probar ruta", Tipo = "Clínico", Estado = EstadoLaboratorio.Abierto,
                Lat = -17.39m, Lng = -66.15m };
            db.Laboratorios.Add(lab); await db.SaveChangesAsync(); return lab.Id;
        });
        var fallback = await anonymous.GetStringAsync($"/laboratorios/Ruta?laboratorioId={labId}"); Assert.Contains("Elegir origen en Google Maps", fallback); Assert.Contains("destination=", fallback);
    }

    [Fact]
    public async Task Concurrent_decisions_apply_once_and_conflicting_assignments_do_not_overlap()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador");
        var id = await Ready(owner, coordinator, "Concurrencia de decisiones");
        var version = await Version(id); var token = await Token(coordinator, $"/Gestion/Detalle/{id}"); var sup = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
        Dictionary<string,string> fields() => new() { ["Id"] = id.ToString(), ["Version"] = version.ToString(), ["Accion"] = "asignar", ["SupervisorId"] = sup.ToString(), ["Motivo"] = "Asignación concurrente", ["__RequestVerificationToken"] = token };
        var results = await Task.WhenAll(coordinator.PostAsync("/Gestion/Decidir", new FormUrlEncodedContent(fields())), coordinator.PostAsync("/Gestion/Decidir", new FormUrlEncodedContent(fields())));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Redirect); Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await app.Db(db => db.Inspecciones.CountAsync(i => i.SolicitudId == id && i.Estado == "pendiente")));
        var a = await Ready(owner, coordinator, "Asignación simultánea A"); var b = await Ready(owner, coordinator, "Asignación simultánea B");
        var fecha = TramiteService.HoraBolivia.AddDays(45).Date.AddHours(9).ToString("s");
        await Task.WhenAll(Decide(coordinator,a,"asignar",new() { ["SupervisorId"]=sup.ToString(),["Fecha"]=fecha }),Decide(coordinator,b,"asignar",new() { ["SupervisorId"]=sup.ToString(),["Fecha"]=fecha }));
        var estados = new[] { await State(a), await State(b) }; Assert.Single(estados,e=>e==EstadoSolicitud.Inspeccion); Assert.Single(estados,e=>e==EstadoSolicitud.Aprobado);
    }

    [Fact]
    public async Task Observed_complement_can_be_retired_without_deleting_history_or_mandatory_documents()
    {
        using var owner=await Login("propietario");using var coordinator=await Login("coordinador");var id=await Draft(owner,"Complemento observado");await UploadRequirements(owner,id);
        await Post(owner,$"/Gestion/Detalle/{id}","/Gestion/Documento",new(){["id"]=id.ToString(),["version"]=(await Version(id)).ToString(),["requisitoId"]="0"},Pdf,"pdf");
        var doc=await app.Db(db=>db.Documentos.SingleAsync(d=>d.SolicitudId==id&&d.RequisitoId==0));await Decide(owner,id,"enviar");
        await Post(coordinator,$"/Gestion/Detalle/{id}","/Gestion/RevisarDocumento",new(){["Id"]=id.ToString(),["DocumentoId"]=doc.Id.ToString(),["Version"]=(await Version(id)).ToString(),["Resultado"]="observado",["Motivo"]="Complemento incorrecto"});await Decide(coordinator,id,"observar_documentos");
        var retirado=await Post(owner,$"/Gestion/Detalle/{id}","/Gestion/RetirarComplementario",new(){["id"]=id.ToString(),["documentoId"]=doc.Id.ToString(),["version"]=(await Version(id)).ToString()});Assert.Equal(HttpStatusCode.Redirect,retirado.StatusCode);
        Assert.False(await app.Db(db=>db.Documentos.Where(d=>d.Id==doc.Id).Select(d=>d.Actual).SingleAsync()));Assert.Equal(HttpStatusCode.OK,(await owner.GetAsync($"/Gestion/Descargar/{doc.Id}")).StatusCode);
        await Decide(owner,id,"enviar");Assert.Equal(EstadoSolicitud.EnRevision,await State(id));
        Assert.True(await app.Db(db=>db.Documentos.AnyAsync(d=>d.SolicitudId==id&&d.RequisitoId!=0&&d.Actual)));
    }

    [Fact]
    public async Task New_document_and_inspection_actions_respect_suspended_permissions()
    {
        using var admin=await Login("administrador");using var coordinator=await Login("coordinador");using var supervisor=await Login("supervisor");
        async Task Change(string role,string key,bool enabled)=>await Post(admin,"/Administracion/Roles","/Administracion/Permiso",new(){["rol"]=role,["clave"]=key,["habilitado"]=enabled.ToString()});
        await Change("Coordinador","documentos",false);await Change("Supervisor","inspecciones",false);
        try {
            var doc=await Post(coordinator,"/auth/Auth/Dashboard","/Gestion/RevisarDocumento",new(){["Id"]="0",["DocumentoId"]="0",["Resultado"]="aprobado",["Motivo"]="Prueba"});Assert.Contains("AccessDenied",doc.Headers.Location!.ToString());
            var prep=await Post(supervisor,"/auth/Auth/Dashboard","/Gestion/PrepararActa",new(){["Id"]="0",["Accion"]="acta",["Motivo"]="Prueba"});Assert.Contains("AccessDenied",prep.Headers.Location!.ToString());
            var citas=await supervisor.GetAsync("/Gestion/Citaciones");Assert.Contains("AccessDenied",citas.Headers.Location!.ToString());
        } finally { await Change("Coordinador","documentos",true);await Change("Supervisor","inspecciones",true); }
    }
}

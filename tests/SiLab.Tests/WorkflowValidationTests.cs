using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class WorkflowTests
{
    private static int slot;
    private async Task<int> InspectionCase(DateTime start, string inspectionState = "pendiente", bool programmed = true, bool checklist = false)
        => await app.Db(async db => {
            var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            var supervisor = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Supervisor);
            var s = new Solicitud { PropietarioId = owner.Id, Municipio = "Sacaba", FechaCreacion = DateTime.UtcNow,
                DatosLaboratorioPropuesto = JsonSerializer.Serialize(new { nombre = "Laboratorio de validación", tipo = "Clínico", coordenadas = new { lat = -17m, lng = -66m } }),
                Estado = EstadoSolicitud.Inspeccion, Version = 1 };
            s.Inspecciones.Add(new Inspeccion { SupervisorId = supervisor.Id, Fecha = start, FechaFin = start.AddHours(2),
                Estado = inspectionState, Programada = programmed, ListaVerificacionJson = checklist ? JsonSerializer.Serialize(new[] {
                    new Verificacion(901,"Limpieza","Referencia",1), new Verificacion(902,"Ventilación","Referencia",1) }) : "[]" });
            db.Add(s); await db.SaveChangesAsync(); return s.Id;
        });

    [Theory]
    [InlineData("pendiente", 0, 60)]
    [InlineData("pendiente", 30, 90)]
    [InlineData("pendiente", -30, 30)]
    [InlineData("pendiente", -30, 150)]
    [InlineData("registrada", 30, 90)]
    [InlineData("validada", 30, 90)]
    [InlineData("rechazada", 30, 90)]
    public async Task Agenda_blocks_identical_partial_enclosing_and_already_reported_intervals(string status, int begin, int end)
    {
        using var supervisor = await Login("supervisor");
        var time = TramiteService.HoraBolivia.Date.AddDays(90 + Interlocked.Increment(ref slot)).AddHours(10);
        await InspectionCase(time, status);
        var id = await InspectionCase(time.AddDays(1), programmed: false);
        var version = await Version(id);
        await Decide(supervisor,id,"programar",new() { ["Fecha"] = time.AddMinutes(begin).ToString("s"), ["FechaFin"] = time.AddMinutes(end).ToString("s") });
        Assert.Equal(version, await Version(id));
        Assert.False(await app.Db(db => db.Inspecciones.Where(i => i.SolicitudId == id).Select(i => i.Programada).SingleAsync()));
    }

    [Theory]
    [InlineData("pendiente", 120)]
    [InlineData("reprogramada", 30)]
    public async Task Agenda_allows_adjacent_visits_and_replaced_reservations(string status, int begin)
    {
        using var supervisor = await Login("supervisor");
        var time = TramiteService.HoraBolivia.Date.AddDays(90 + Interlocked.Increment(ref slot)).AddHours(10);
        await InspectionCase(time,status); var id = await InspectionCase(time.AddDays(1),programmed:false);
        await Decide(supervisor,id,"programar",new() { ["Fecha"] = time.AddMinutes(begin).ToString("s"), ["FechaFin"] = time.AddMinutes(begin+60).ToString("s") });
        Assert.True(await app.Db(db => db.Inspecciones.AnyAsync(i => i.SolicitudId == id && i.Programada && i.Estado == "pendiente")));
    }

    [Fact]
    public async Task Reprogramming_replaces_only_pending_booking_preserves_history_and_checks_own_completed_visits()
    {
        using var supervisor = await Login("supervisor");
        var time = TramiteService.HoraBolivia.Date.AddDays(90 + Interlocked.Increment(ref slot)).AddHours(10);
        var id = await InspectionCase(time);
        await Decide(supervisor,id,"programar",new() { ["Fecha"] = time.AddMinutes(30).ToString("s"), ["FechaFin"] = time.AddMinutes(90).ToString("s") });
        Assert.Equal(2,await app.Db(db => db.Inspecciones.CountAsync(i => i.SolicitudId == id)));
        await app.Db(async db => { var old = await db.Inspecciones.FirstAsync(i => i.SolicitudId == id && i.Estado == "reprogramada"); old.Estado = "validada"; return await db.SaveChangesAsync(); });
        var version = await Version(id);
        await Decide(supervisor,id,"programar",new() { ["Fecha"] = time.ToString("s"), ["FechaFin"] = time.AddHours(1).ToString("s") });
        Assert.Equal(version,await Version(id));
    }

    private static Dictionary<string,string> Checks(string first = "cumple", string second = "cumple") => new() {
        ["Motivo"] = "", ["Verificaciones[0].Id"] = "901", ["Verificaciones[0].Resultado"] = first,
        ["Verificaciones[1].Id"] = "902", ["Verificaciones[1].Resultado"] = second };

    [Theory]
    [InlineData("no_aplica", "no_aplica", "aprobado", "No corresponde", true)]
    [InlineData("observado", "cumple", "aprobado", "Corregir limpieza", true)]
    [InlineData("observado", "cumple", "observado", "", true)]
    [InlineData("cumple", "cumple", "aprobado", "", false)]
    public async Task Acta_rejects_inconsistent_incomplete_or_unsigned_submission_without_mutation(string first, string second, string result, string observation, bool signed)
    {
        using var supervisor = await Login("supervisor");
        var id = await InspectionCase(TramiteService.HoraBolivia.AddHours(-1),checklist:true);
        var fields = Checks(first,second); fields["Resultado"] = result; fields["Verificaciones[0].Observacion"] = observation;
        var response = await Decide(supervisor,id,"acta",fields,signed ? Pdf : null);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        Assert.Equal(1,await Version(id)); Assert.Equal(EstadoSolicitud.Inspeccion,await State(id));
        Assert.Contains("acta-data",await response.Content.ReadAsStringAsync());
        Assert.Equal("",await app.Db(db => db.Inspecciones.Where(i => i.SolicitudId == id).Select(i => i.ActaPdf).SingleAsync()));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("not-json")]
    public async Task Unreadable_historical_checklists_block_progress_with_recoverable_message(string snapshot)
    {
        using var supervisor = await Login("supervisor");
        var id = await InspectionCase(TramiteService.HoraBolivia.AddHours(-1),checklist:true);
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id); i.ListaVerificacionJson=snapshot; return await db.SaveChangesAsync(); });
        var response = await supervisor.GetAsync($"/Gestion/Detalle/{id}");
        Assert.Equal(HttpStatusCode.Conflict,response.StatusCode); Assert.Contains("Volver a mis",await response.Content.ReadAsStringAsync());
        Assert.Equal(1,await Version(id));
    }

    [Theory]
    [InlineData("901", "901", "cumple")]
    [InlineData("999", "902", "cumple")]
    [InlineData("901", "902", "inventado")]
    public async Task Acta_rejects_forged_checklist_ids_or_results(string first, string second, string result)
    {
        using var supervisor = await Login("supervisor");
        var id = await InspectionCase(TramiteService.HoraBolivia.AddHours(-1),checklist:true);
        var fields = Checks(); fields["Verificaciones[0].Id"] = first; fields["Verificaciones[1].Id"] = second;
        fields["Verificaciones[0].Resultado"] = result;
        Assert.Equal(HttpStatusCode.BadRequest,(await Decide(supervisor,id,"acta",fields,Pdf)).StatusCode);
        Assert.Equal(1,await Version(id));
    }

    [Fact]
    public async Task Scheduling_extreme_date_does_not_crash_and_acta_cannot_precede_visit()
    {
        using var supervisor = await Login("supervisor");
        var id = await InspectionCase(TramiteService.HoraBolivia.AddDays(10),checklist:true);
        Assert.Equal(HttpStatusCode.BadRequest,(await Decide(supervisor,id,"acta",Checks(),Pdf)).StatusCode);
        var response = await Decide(supervisor,id,"programar",new() { ["Fecha"]="9999-12-31T18:00", ["FechaFin"]="9999-12-31T19:00" });
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode); Assert.Equal(1,await Version(id));
    }

    [Fact]
    public async Task Field_rejection_still_blocks_duplicate_renewal_until_existing_case_is_resolved()
    {
        using var owner = await Login("propietario");
        var labId = await app.Db(async db => {
            var user = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            var lab = new Laboratorio { Nombre="Renovación pendiente de campo",Tipo="Clínico",Municipio="Sacaba",PropietarioId=user.Id,
                DatosVerificados=true,VigenciaHasta=DateTime.UtcNow.AddDays(5) };
            db.Add(lab); await db.SaveChangesAsync();
            db.Add(new Solicitud { PropietarioId=user.Id,LaboratorioId=lab.Id,EsRenovacion=true,Estado=EstadoSolicitud.Rechazado,
                MotivoRechazo="campo",FechaCreacion=DateTime.UtcNow,DatosLaboratorioPropuesto="{}" });
            await db.SaveChangesAsync(); return lab.Id;
        });
        await Post(owner,"/auth/Auth/Dashboard","/Gestion/Renovar",new() { ["id"]=labId.ToString() });
        Assert.Equal(1,await app.Db(db => db.Solicitudes.CountAsync(s => s.LaboratorioId == labId)));
    }

    [Fact]
    public async Task Acta_derives_summary_once_and_clears_notes_from_compliant_items()
    {
        using var supervisor = await Login("supervisor");
        var id = await InspectionCase(TramiteService.HoraBolivia.AddHours(-1),checklist:true);
        var fields = Checks("observado"); fields["Verificaciones[0].Observacion"] = "  Reparar lavamanos  ";
        fields["Verificaciones[1].Observacion"] = "Observación vieja que ya no aplica";
        var response = await Decide(supervisor,id,"acta",fields,Pdf);
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        await app.Db(async db => {
            var inspection = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id);
            Assert.Equal("Limpieza: Reparar lavamanos",inspection.Acta);
            Assert.Equal("",inspection.NotasAdicionales); Assert.Equal("observado",inspection.Resultado);
            Assert.NotNull(inspection.PlazoSubsanacion);
            Assert.Equal("",ExpedienteService.LeerVerificaciones(inspection)[1].Observacion); return 0;
        });
    }

    [Fact]
    public async Task Acta_does_not_require_duplicate_general_observations_for_compliant_checklist()
    {
        using var supervisor = await Login("supervisor");
        var id = await InspectionCase(TramiteService.HoraBolivia.AddHours(-1),checklist:true);
        var response = await Decide(supervisor,id,"acta",Checks(),Pdf);
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id);
            Assert.Equal("aprobado",i.Resultado); Assert.Null(i.PlazoSubsanacion); Assert.NotEmpty(i.Acta); return 0; });
    }

    [Fact]
    public async Task Owner_can_repair_draft_preserving_files_but_cannot_overwrite_stale_or_submitted_data()
    {
        using var owner = await Login("propietario"); var id = await Draft(owner); await UploadRequirements(owner,id);
        var docs = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id).Select(d => d.ArchivoPdf).ToListAsync());
        var fields = new Dictionary<string,string> { ["Id"] = id.ToString(), ["Version"] = (await Version(id)).ToString(),
            ["NombreLaboratorio"] = "Laboratorio corregido", ["TipoLaboratorio"] = "Clínico", ["Municipio"] = "Sacaba", ["Latitud"] = "-17.4", ["Longitud"] = "-66.1" };
        var result = await Post(owner,$"/Gestion/EditarDatos/{id}","/Gestion/EditarDatos",new(fields)); Assert.Equal(HttpStatusCode.Redirect,result.StatusCode);
        Assert.Equal("Laboratorio corregido",await app.Db(async db => ExpedienteService.ValidarDatos(await db.Solicitudes.FindAsync(id) ?? throw new Exception()).Nombre));
        Assert.Equal(docs,await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id).Select(d => d.ArchivoPdf).ToListAsync()));
        Assert.Equal(HttpStatusCode.Conflict,(await Post(owner,$"/Gestion/EditarDatos/{id}","/Gestion/EditarDatos",new(fields))).StatusCode);
        fields["Version"] = (await Version(id)).ToString(); fields["Latitud"] = "100";
        Assert.Equal(HttpStatusCode.BadRequest,(await Post(owner,$"/Gestion/EditarDatos/{id}","/Gestion/EditarDatos",new(fields))).StatusCode);
        await Decide(owner,id,"enviar"); Assert.Equal(EstadoSolicitud.EnRevision,await State(id));
        fields["Latitud"] = "-17";
        Assert.Equal(HttpStatusCode.BadRequest,(await Post(owner,"/auth/Auth/Dashboard","/Gestion/EditarDatos",fields)).StatusCode);
    }

    [Fact]
    public async Task Document_approval_accepts_no_repeated_reason_but_observation_requires_correction_text()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador");
        var id = await Draft(owner); await UploadRequirements(owner,id); await Decide(owner,id,"enviar");
        var doc = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id && d.Actual).Select(d => d.Id).FirstAsync());
        var fields = new Dictionary<string,string> { ["Id"]=id.ToString(),["DocumentoId"]=doc.ToString(),["Version"]=(await Version(id)).ToString(),["Resultado"]="observado",["Motivo"]="" };
        Assert.Equal(HttpStatusCode.BadRequest,(await Post(coordinator,$"/Gestion/Detalle/{id}","/Gestion/RevisarDocumento",new(fields))).StatusCode);
        fields["Resultado"]="aprobado";
        Assert.Equal(HttpStatusCode.Redirect,(await Post(coordinator,$"/Gestion/Detalle/{id}","/Gestion/RevisarDocumento",fields)).StatusCode);
        Assert.Equal("aprobado",await app.Db(db => db.Documentos.Where(d => d.Id == doc).Select(d => d.EstadoRevision).SingleAsync()));
    }

    [Fact]
    public async Task Legal_observation_targets_only_selected_documents_and_keeps_decision_visible()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador");
        using var supervisor = await Login("supervisor"); using var lawyer = await Login("abogado");
        var id = await Legal(owner,coordinator,supervisor,"Devolución legal selectiva");
        var ids = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id && d.Actual).Select(d => d.Id).ToListAsync());
        var version = await Version(id); await Decide(lawyer,id,"observar_legal"); Assert.Equal(version,await Version(id));
        await Decide(lawyer,id,"observar_legal",new() { ["DocumentosObservados[0]"] = ids[0].ToString() });
        Assert.Equal(EstadoSolicitud.Observado,await State(id));
        Assert.Equal(1,await app.Db(db => db.Documentos.CountAsync(d => d.SolicitudId == id && d.EstadoRevision == "observado")));
        Assert.Equal(ids.Count-1,await app.Db(db => db.Documentos.CountAsync(d => d.SolicitudId == id && d.EstadoRevision == "aprobado")));
        Assert.Equal(HttpStatusCode.OK,(await lawyer.GetAsync($"/Gestion/Detalle/{id}")).StatusCode);
    }

    [Fact]
    public async Task Admin_rejects_stale_user_edits_and_manager_can_recover_from_invalid_dates()
    {
        using var admin = await Login("administrador"); using var manager = await Login("gerente");
        var u = await app.Db(async db => { var user = new Usuario { Nombre="Versión original", Email=Guid.NewGuid()+"@example.test", Rol=RolUsuario.Propietario, Password="hash" }; db.Add(user); await db.SaveChangesAsync(); return user; });
        var fields = new Dictionary<string,string> { ["Id"]=u.Id.ToString(), ["Version"]=u.Version.ToString(), ["Nombre"]="Primera edición", ["Email"]=u.Email, ["Rol"]="Propietario", ["Activo"]="true" };
        Assert.Equal(HttpStatusCode.Redirect,(await Post(admin,$"/Administracion/Usuario/{u.Id}","/Administracion/Usuario",new(fields))).StatusCode);
        fields["Nombre"]="Edición obsoleta";
        Assert.Equal(HttpStatusCode.Conflict,(await Post(admin,$"/Administracion/Usuario/{u.Id}","/Administracion/Usuario",fields)).StatusCode);
        Assert.Equal("Primera edición",await app.Db(db => db.Usuarios.Where(x => x.Id == u.Id).Select(x => x.Nombre).SingleAsync()));
        var response = await manager.GetAsync("/Metricas?desde=2026-12-01&hasta=2026-01-01");
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode); Assert.Contains("Consultar",await response.Content.ReadAsStringAsync());
    }
}

public class SeedDisplayRepairTests
{
    [Fact]
    public async Task Display_repair_preserves_ids_custom_names_evidence_and_all_public_records()
    {
        using var app = new TestApplication { SeedDemoData = true }; using var client = app.Browser();
        var original = await app.Db(async db => {
            var s = await db.Solicitudes.Include(s => s.Laboratorio).SingleAsync(s => s.Codigo == "DEMO-HABILITADO");
            var lab = s.Laboratorio!; lab.Tipo = "Clínico (DEMO)"; lab.Municipio = "Cochabamba (DEMO)";
            var cat = await db.Catalogos.SingleAsync(c => c.Clase == "tipo"); cat.Nombre = "Clínico (DEMO)";
            lab.Nombre = "DEMO - Consultar licencia y servicios"; lab.Servicios = "DEMO: Hematología, Química clínica";
            var json = System.Text.Json.Nodes.JsonNode.Parse(s.DatosLaboratorioPropuesto)!; json["nombre"] = lab.Nombre; json["tipo"] = "Clínico (DEMO)"; s.Municipio = "Cochabamba (DEMO)"; s.DatosLaboratorioPropuesto = json.ToJsonString();
            var renewal = new Solicitud { PropietarioId=s.PropietarioId,LaboratorioId=lab.Id,EsRenovacion=true,Codigo=lab.Codigo,
                DatosLaboratorioPropuesto=s.DatosLaboratorioPropuesto,Municipio=s.Municipio,Estado=EstadoSolicitud.Borrador,FechaCreacion=DateTime.UtcNow };
            db.Add(renewal);
            var custom = await db.Solicitudes.Include(s => s.Laboratorio).SingleAsync(s => s.Codigo == "DEMO-RENOVACION");
            custom.Laboratorio!.Nombre = "Nombre personalizado conservado";
            var count = await db.Laboratorios.CountAsync(); var pdf = s.InformeCierrePdf; await db.SaveChangesAsync();
            return (s.Id, LabId:lab.Id, Count:count, Pdf:pdf, RenewalId:renewal.Id);
        });
        for(var n=0;n<2;n++) await app.Db(async db => { await DemoDataSeeder.CorregirRotulosAsync(db); return 0; });
        await app.Db(async db => { var s = await db.Solicitudes.Include(s => s.Laboratorio).SingleAsync(s => s.Id == original.Id);
            Assert.Equal("Clínico",s.Laboratorio!.Tipo); Assert.Equal("Cochabamba",s.Municipio); Assert.Equal("Clínico",ExpedienteService.ValidarDatos(s).Tipo);
            Assert.Equal(original.LabId,s.LaboratorioId); Assert.Equal("Laboratorio Central",s.Laboratorio!.Nombre);
            Assert.Equal(s.Laboratorio.Nombre,ExpedienteService.ValidarDatos(s).Nombre); Assert.Equal(original.Pdf,s.InformeCierrePdf);
            Assert.Equal(s.Laboratorio.Nombre,ExpedienteService.ValidarDatos(await db.Solicitudes.SingleAsync(x => x.Id == original.RenewalId)).Nombre);
            Assert.Equal(original.Count,await db.Laboratorios.CountAsync());
            Assert.Equal("Nombre personalizado conservado",(await db.Laboratorios.SingleAsync(l => l.Codigo == "DEMO-LAB-RENOVACION")).Nombre); return 0; });
        var html = await client.GetStringAsync("/publico/Publico/Index");
        Assert.Contains("Laboratorio Central",html); Assert.Contains("Nombre personalizado conservado",html);
        Assert.DoesNotContain("DEMO - Consultar licencia",html); Assert.DoesNotContain("DEMO: Hematología",html);
    }
}

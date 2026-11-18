using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;
using Xunit;

namespace SiLab.Tests;

public sealed class OperationsTests(TestApplication app) : IClassFixture<TestApplication>
{
    private static async Task<string> Token(HttpClient c, string page = "/auth/Auth/Dashboard")
    {
        var html = await c.GetStringAsync(page);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success); return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private async Task<HttpClient> Login(string role)
    {
        var c = app.Browser(); var r = await c.PostAsync("/auth/Auth/Login", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["Email"] = role + "@si-lab.local", ["Password"] = "Demo123!", ["__RequestVerificationToken"] = await Token(c, "/auth/Auth/Login") }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode); return c;
    }
    private static string Datos(string nombre = "Laboratorio de operaciones", string tipo = "Clínico") => JsonSerializer.Serialize(new { nombre, tipo, coordenadas = new { lat = -17.39, lng = -66.15 } });

    [Fact]
    public async Task Charts_use_saved_cohort_zero_fill_months_and_weight_closures_correctly()
    {
        using var manager = await Login("gerente"); var municipality = "Métricas-" + Guid.NewGuid().ToString("N");
        await app.Db(async db => {
            var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            Solicitud New(DateTime sent, EstadoSolicitud state, int? days, bool renewal = false) => new()
            { PropietarioId = owner.Id, Municipio = municipality, DatosLaboratorioPropuesto = Datos(), FechaCreacion = sent, PrimerEnvio = sent,
                Estado = state, FechaCierre = days.HasValue ? sent.AddDays(days.Value) : null, EsRenovacion = renewal };
            var field = New(new DateTime(2026, 1, 31, 23, 59, 0), EstadoSolicitud.Rechazado, null); field.MotivoRechazo = "campo";
            var definitive = New(new DateTime(2026, 3, 2), EstadoSolicitud.Rechazado, 5); definitive.MotivoRechazo = "legal";
            var draft = New(new DateTime(2026, 1, 15), EstadoSolicitud.Borrador, null); draft.PrimerEnvio = null;
            db.AddRange(New(new DateTime(2026, 1, 10), EstadoSolicitud.Habilitado, 2), New(new DateTime(2026, 3, 1), EstadoSolicitud.Habilitado, 8, true), field, definitive, draft,
                New(new DateTime(2025, 12, 31), EstadoSolicitud.EnRevision, null));
            return await db.SaveChangesAsync();
        });
        var query = "?desde=2026-01-01&hasta=2026-03-31&municipio=" + Uri.EscapeDataString(municipality);
        var response = await manager.GetAsync("/Metricas/Datos" + query); Assert.True(response.Headers.CacheControl?.NoStore);
        var data = (await response.Content.ReadFromJsonAsync<MetricasVisuales>())!;
        Assert.Equal(4, data.Total); Assert.Equal(2, data.Habilitadas); Assert.Equal(1, data.Rechazadas); Assert.Equal(1, data.Pendientes);
        Assert.Equal(1, data.Borradores); Assert.Equal(5, data.PromedioDias); Assert.Equal(3, data.CierresConFecha);
        Assert.Equal(200d / 3, data.TasaAprobacion!.Value, 6);
        Assert.Equal(3, data.Meses.Count); Assert.Equal(new MetricaMes("2026-02", 0, 0), data.Meses[1]);
        Assert.Equal(data.Total, data.Meses.Sum(m => m.Aperturas + m.Renovaciones));
        Assert.Equal(data.Total, data.Tipos.Sum(m => m.Cantidad));
        var csv = await manager.GetStringAsync("/Metricas" + query + "&descargar=true"); Assert.Contains(",4,2,1,1,3,1,5.00,66.67,", csv);
        await app.Db(async db => { var s = await db.Solicitudes.FirstAsync(s => s.Municipio == municipality && s.Estado == EstadoSolicitud.Borrador); s.Estado = EstadoSolicitud.EnRevision; s.PrimerEnvio = new DateTime(2026, 3, 3); return await db.SaveChangesAsync(); });
        var updated = (await manager.GetFromJsonAsync<MetricasVisuales>("/Metricas/Datos" + query))!;
        Assert.Equal(5, updated.Total); Assert.Equal(0, updated.Borradores);
    }

    [Fact]
    public async Task Empty_charts_have_no_fake_rates_or_durations_and_do_not_serialize_user_secrets()
    {
        using var manager = await Login("gerente"); var path = "/Metricas/Datos?municipio=" + Guid.NewGuid();
        var json = await manager.GetStringAsync(path); var data = JsonSerializer.Deserialize<MetricasVisuales>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(0, data.Total); Assert.Null(data.PromedioDias); Assert.Null(data.TasaAprobacion); Assert.Empty(data.Tipos);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("selloSeguridad", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.GetAsync("/Metricas/Datos?desde=no-fecha")).StatusCode);
    }

    [Theory]
    [InlineData("propietario", "/Metricas/Datos")]
    [InlineData("supervisor", "/Metricas/Datos")]
    [InlineData("gerente", "/Gestion/Actas")]
    [InlineData("coordinador", "/Gestion/Rutas")]
    public async Task Interactive_surfaces_keep_role_authorization(string role, string path)
    {
        using var c = await Login(role); Assert.Contains("AccessDenied", (await c.GetAsync(path)).Headers.Location!.ToString());
    }

    [Fact]
    public async Task New_routes_and_metrics_endpoint_respect_suspended_permissions()
    {
        using var admin = await Login("administrador"); using var manager = await Login("gerente"); using var supervisor = await Login("supervisor");
        async Task Change(string rol, string clave, bool enabled) {
            var r = await admin.PostAsync("/Administracion/Permiso", new FormUrlEncodedContent(new Dictionary<string,string>
            { ["rol"] = rol, ["clave"] = clave, ["habilitado"] = enabled.ToString(), ["__RequestVerificationToken"] = await Token(admin) })); Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        }
        try {
            await Change("Gerente", "metricas", false); await Change("Supervisor", "inspecciones", false);
            foreach(var path in new[] { "/Gestion/Actas", "/Gestion/Rutas", "/Gestion/Agenda" }) Assert.Contains("AccessDenied", (await supervisor.GetAsync(path)).Headers.Location!.ToString());
            Assert.Contains("AccessDenied", (await manager.GetAsync("/Metricas/Datos")).Headers.Location!.ToString());
        } finally { await Change("Gerente", "metricas", true); await Change("Supervisor", "inspecciones", true); }
    }

    [Fact]
    public async Task Supervisor_views_only_own_visits_and_gets_a_focused_detail()
    {
        using var supervisor = await Login("supervisor");
        var ids = await app.Db(async db => {
            var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario); var user = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Supervisor);
            var other = new Usuario { Nombre = "Supervisor ajeno", Email = Guid.NewGuid() + "@example.test", Rol = RolUsuario.Supervisor, Password = "hash-no-usado" }; db.Add(other); await db.SaveChangesAsync();
            var own = new Solicitud { PropietarioId = owner.Id, Municipio = "Sacaba", Estado = EstadoSolicitud.Inspeccion, Version = 1, DatosLaboratorioPropuesto = Datos("Visita propia"), FechaCreacion = DateTime.UtcNow };
            var foreign = new Solicitud { PropietarioId = owner.Id, Municipio = "Sacaba", Estado = EstadoSolicitud.Inspeccion, Version = 1, DatosLaboratorioPropuesto = Datos("Visita ajena secreta"), FechaCreacion = DateTime.UtcNow };
            own.Inspecciones.Add(new Inspeccion { SupervisorId = user.Id, Programada = false }); foreign.Inspecciones.Add(new Inspeccion { SupervisorId = other.Id, Programada = false });
            db.AddRange(own, foreign); await db.SaveChangesAsync(); return new[] { own.Id, foreign.Id };
        });
        foreach(var path in new[] { "/Gestion/Agenda", "/Gestion/Rutas" }) {
            var html = await supervisor.GetStringAsync(path); Assert.Contains("Visita propia", html); Assert.DoesNotContain("Visita ajena secreta", html); Assert.DoesNotContain("password", html, StringComparison.OrdinalIgnoreCase);
        }
        var detail = await supervisor.GetStringAsync($"/Gestion/Detalle/{ids[0]}");
        Assert.Contains("Elegir fecha y horario", detail); Assert.Contains("Primero programe la visita", detail);
        Assert.DoesNotContain("Agregue PDF uno por uno", detail); Assert.DoesNotContain("Enviar solicitud", detail);
        Assert.Equal(HttpStatusCode.NotFound, (await supervisor.GetAsync($"/Gestion/Detalle/{ids[1]}")).StatusCode);
        var date = TramiteService.HoraBolivia.AddDays(31).Date.AddHours(10);
        var r = await supervisor.PostAsync("/Gestion/Decidir", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["Id"] = ids[0].ToString(), ["Version"] = "1", ["Accion"] = "programar", ["Fecha"] = date.ToString("yyyy-MM-ddTHH:mm"),
            ["FechaFin"] = date.AddHours(1).ToString("yyyy-MM-ddTHH:mm"), ["Motivo"] = "Programación desde la vista del supervisor", ["__RequestVerificationToken"] = await Token(supervisor, $"/Gestion/Detalle/{ids[0]}") }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.True(await app.Db(db => db.Inspecciones.AnyAsync(i => i.SolicitudId == ids[0] && i.Estado == "pendiente" && i.Programada && i.Fecha == date)));
    }

    [Fact]
    public async Task Acts_citations_and_form_snapshot_belong_to_the_current_supervisor()
    {
        using var supervisor = await Login("supervisor");
        var info = await app.Db(async db => {
            var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            var actor = await db.Usuarios.FirstAsync(u => u.Email == "supervisor@si-lab.local");
            var other = new Usuario { Nombre = "Otro supervisor", Email = Guid.NewGuid() + "@example.test", Password = "no-usado", Rol = RolUsuario.Supervisor };
            db.Add(other); await db.SaveChangesAsync();
            var s = new Solicitud { PropietarioId = owner.Id, DatosLaboratorioPropuesto = Datos("Expediente compartido en el tiempo"), Municipio = "Sacaba", Estado = EstadoSolicitud.Observado, OrigenObservacion = "campo", FechaCreacion = DateTime.UtcNow };
            var mine = new Inspeccion { SupervisorId = actor.Id, Fecha = TramiteService.HoraBolivia.AddDays(-3), Estado = "validada", Resultado = "observado",
                ListaVerificacionJson = JsonSerializer.Serialize(new[] { new Verificacion(1, "Punto del supervisor actual", "", 1) }) };
            s.Inspecciones.Add(mine); db.Add(s); await db.SaveChangesAsync();
            var foreign = new Inspeccion { SolicitudId = s.Id, SupervisorId = other.Id, Fecha = TramiteService.HoraBolivia.AddDays(-1), Estado = "validada", Resultado = "observado",
                ListaVerificacionJson = JsonSerializer.Serialize(new[] { new Verificacion(2, "Punto de otro supervisor", "", 1) }) };
            db.Add(foreign); await db.SaveChangesAsync();
            db.AddRange(new Citacion { SolicitudId = s.Id, InspeccionId = mine.Id, SupervisorId = actor.Id, Motivo = "Cita propia", FechaEmision = TramiteService.HoraBolivia.Date, Vencimiento = TramiteService.HoraBolivia.AddDays(10) },
                new Citacion { SolicitudId = s.Id, InspeccionId = foreign.Id, SupervisorId = other.Id, Motivo = "Cita ajena", FechaEmision = TramiteService.HoraBolivia.Date, Vencimiento = TramiteService.HoraBolivia.AddDays(10) });
            await db.SaveChangesAsync(); return (s.Id, Mine: mine.Id, Other: foreign.Id);
        });
        var acts = await supervisor.GetStringAsync("/Gestion/Actas");
        Assert.Contains($"Acta #{info.Mine} · solicitud", acts); Assert.DoesNotContain($"Acta #{info.Other} · solicitud", acts);
        var citations = await supervisor.GetStringAsync("/Gestion/Citaciones");
        Assert.Contains($"/Gestion/Detalle/{info.Id}", citations);
        var citationLinks = Regex.Matches(citations, @"/Gestion/Descargar/(\d+)\?tipo=citacion").Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        var ownIds = await app.Db(db => db.Citaciones.Where(c => c.SupervisorId == db.Usuarios.Where(u => u.Email == "supervisor@si-lab.local").Select(u => u.Id).Single()).Select(c => c.Id).ToListAsync());
        Assert.All(citationLinks, id => Assert.Contains(id, ownIds));
        var pdf = System.Text.Encoding.Latin1.GetString(await supervisor.GetByteArrayAsync($"/Gestion/FormularioActa/{info.Id}?resumen=true"));
        Assert.Contains("Punto del supervisor actual", pdf); Assert.DoesNotContain("Punto de otro supervisor", pdf);
    }
}

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using SEDESLABORATORIO;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public sealed class TestApplication : WebApplicationFactory<Program>
{
    public bool SeedDemoData { get; init; }
    private readonly string directory = Path.Combine(Path.GetTempPath(), "silab-tests-" + Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(directory);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(directory, "tests.db")}",
            ["Storage:Root"] = Path.Combine(directory, "documents"), ["Auth:SeedDemoUsers"] = "true",
            ["Auth:SeedDemoData"] = SeedDemoData.ToString()
        }));
        builder.ConfigureServices(services => { services.RemoveAll<IHostedService>(); services.AddSingleton<IStartupFilter, TestClientAddressFilter>(); });
    }
    public async Task<T> Db<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
    private static int browserNumber;
    public HttpClient Browser()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        var n = Interlocked.Increment(ref browserNumber);
        client.DefaultRequestHeaders.Add("X-Test-Client", $"10.0.{n / 250 % 250}.{n % 250 + 1}");
        return client;
    }
}

// Solo TestServer: clientes distintos tienen IP distintas sin desactivar el límite real de login.
public sealed class TestClientAddressFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, action) => { if(IPAddress.TryParse(context.Request.Headers["X-Test-Client"], out var address)) context.Connection.RemoteIpAddress = address; await action(); });
        next(app);
    };
}

public partial class WorkflowTests(TestApplication app) : IClassFixture<TestApplication>
{
    private static readonly byte[] Pdf = SimplePdf.Create(["Documento de prueba", "Firma de prueba"]);
    private static async Task<string> Token(HttpClient client, string page)
    {
        var response = await client.GetAsync(page);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Falta token antiforgery en " + page);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private async Task<HttpClient> Login(string role)
    {
        var client = app.Browser();
        var response = await client.PostAsync("/auth/Auth/Login", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["Email"] = role + "@si-lab.local", ["Password"] = "Demo123!", ["__RequestVerificationToken"] = await Token(client, "/auth/Auth/Login") }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Dashboard", response.Headers.Location!.ToString());
        return client;
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string page, string target, Dictionary<string,string> values, byte[]? file = null, string fileField = "Pdf", string fileName = "documento.pdf")
    {
        values["__RequestVerificationToken"] = await Token(client, page);
        using var content = new MultipartFormDataContent();
        foreach (var pair in values) content.Add(new StringContent(pair.Value), pair.Key);
        if (file is not null) content.Add(new ByteArrayContent(file), fileField, fileName);
        return await client.PostAsync(target, content);
    }
    private async Task<int> Draft(HttpClient owner, string name = "Laboratorio de integración")
    {
        var result = await Post(owner, "/solicitud-registro/SolicitudRegistro/Nueva", "/solicitud-registro/SolicitudRegistro/Nueva", new()
        { ["NombreLaboratorio"] = name, ["TipoLaboratorio"] = "Clínico", ["Municipio"] = "Sacaba", ["Latitud"] = "-17.39", ["Longitud"] = "-66.15" });
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        return int.Parse(Regex.Match(result.Headers.Location!.ToString(), @"\d+$").Value);
    }
    private Task<int> Version(int id) => app.Db(db => db.Solicitudes.Where(s => s.Id == id).Select(s => s.Version).SingleAsync());
    private Task<EstadoSolicitud> State(int id) => app.Db(db => db.Solicitudes.Where(s => s.Id == id).Select(s => s.Estado).SingleAsync());
    private async Task UploadRequirements(HttpClient owner, int id)
    {
        var requirements = await app.Db(db => db.Requisitos.Where(r => r.Activo && r.Obligatorio).Select(r => r.Id).ToListAsync());
        foreach (var r in requirements)
        {
            var result = await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/Documento", new() { ["id"] = id.ToString(), ["version"] = (await Version(id)).ToString(), ["requisitoId"] = r.ToString() }, Pdf, "pdf");
            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        }
    }
    private async Task<HttpResponseMessage> Decide(HttpClient client, int id, string action, Dictionary<string,string>? extra = null, byte[]? file = null)
    {
        var values = extra ?? new(); values["Id"] = id.ToString(); values["Version"] = (await Version(id)).ToString(); values["Accion"] = action;
        values.TryAdd("Motivo", "Decisión fundamentada de prueba");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Gestion/Detalle/{id}")).StatusCode);
        return await Post(client, "/auth/Auth/Dashboard", "/Gestion/Decidir", values, file);
    }

    private async Task ReviewDocuments(HttpClient coordinator, int id)
    {
        var ids = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id && d.Actual && d.EstadoRevision != "aprobado").Select(d => d.Id).ToListAsync());
        foreach (var docId in ids)
        {
            var response = await Post(coordinator, $"/Gestion/Detalle/{id}", "/Gestion/RevisarDocumento", new()
            { ["Id"] = id.ToString(), ["DocumentoId"] = docId.ToString(), ["Version"] = (await Version(id)).ToString(), ["Resultado"] = "aprobado", ["Motivo"] = "PDF individual revisado y conforme" });
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
    }

    [Fact]
    public async Task Complete_opening_renewal_and_role_workflow()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador");
        using var supervisor = await Login("supervisor"); using var lawyer = await Login("abogado");
        var id = await Draft(owner); Assert.Equal(EstadoSolicitud.Borrador, await State(id));
        await Decide(owner, id, "enviar"); Assert.Equal(EstadoSolicitud.Borrador, await State(id));
        await UploadRequirements(owner, id);
        await Decide(owner, id, "enviar"); Assert.Equal(EstadoSolicitud.EnRevision, await State(id));
        await Decide(owner, id, "aprobar_documentos"); Assert.Equal(EstadoSolicitud.EnRevision, await State(id));
        await Decide(coordinator, id, "aprobar_documentos"); Assert.Equal(EstadoSolicitud.EnRevision, await State(id));
        await ReviewDocuments(coordinator, id);
        await Decide(coordinator, id, "aprobar_documentos"); Assert.Equal(EstadoSolicitud.Aprobado, await State(id));
        var supervisorId = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = TramiteService.HoraBolivia.AddDays(1).ToString("yyyy-MM-ddTHH:mm") });
        Assert.Equal(EstadoSolicitud.Inspeccion, await State(id));
        Assert.Equal(HttpStatusCode.OK, (await supervisor.GetAsync("/Gestion/Agenda")).StatusCode);
        var form = await supervisor.GetAsync($"/Gestion/FormularioActa/{id}"); Assert.Equal("application/pdf", form.Content.Headers.ContentType!.MediaType);
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "aprobado" }, Pdf);
        Assert.Equal(EstadoSolicitud.Inspeccion, await State(id)); // fecha futura bloqueada
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id); i.Fecha = TramiteService.HoraBolivia.AddMinutes(-5); return await db.SaveChangesAsync(); });
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "observado", ["Citacion"] = "Presentar subsanación", ["DiasSubsanacion"] = "15" }, Pdf);
        Assert.Equal(EstadoSolicitud.RevisionActa, await State(id));
        await Decide(coordinator, id, "aprobar_acta"); Assert.Equal(EstadoSolicitud.Observado, await State(id));
        Assert.True(await app.Db(db => db.Inspecciones.AnyAsync(i => i.SolicitudId == id && i.PlazoSubsanacion != null && i.Citacion != "")));
        await Decide(owner, id, "enviar");
        await Decide(coordinator, id, "aprobar_documentos");
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = TramiteService.HoraBolivia.AddDays(2).ToString("yyyy-MM-ddTHH:mm") });
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id && i.Estado == "pendiente"); i.Fecha = TramiteService.HoraBolivia.AddMinutes(-1); return await db.SaveChangesAsync(); });
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "aprobado" }, Pdf);
        await Decide(coordinator, id, "aprobar_acta"); Assert.Equal(EstadoSolicitud.RevisionLegal, await State(id));
        await Decide(lawyer, id, "aprobar_legal"); Assert.Equal(EstadoSolicitud.RevisionLegal, await State(id));
        await Decide(lawyer, id, "aprobar_legal", file: Pdf); Assert.Equal(EstadoSolicitud.Habilitado, await State(id));
        var laboratory = await app.Db(db => db.Solicitudes.Include(s => s.Laboratorio).Where(s => s.Id == id).Select(s => s.Laboratorio!).SingleAsync());
        Assert.True(laboratory.VigenciaHasta > DateTime.UtcNow.AddDays(364));
        Assert.Equal(EstadoLaboratorio.Abierto, laboratory.Estado);
        var report = await coordinator.GetAsync($"/Gestion/Informe/{id}"); Assert.Equal("application/pdf", report.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(await report.Content.ReadAsByteArrayAsync()));
        var originalExpiry = laboratory.VigenciaHasta;
        await Decide(lawyer, id, "aprobar_legal", file: Pdf); // no cierre doble
        Assert.Equal(originalExpiry, await app.Db(db => db.Laboratorios.Where(l => l.Id == laboratory.Id).Select(l => l.VigenciaHasta).SingleAsync()));
        var renew = await Post(owner, "/Gestion/Establecimientos", "/Gestion/Renovar", new() { ["id"] = laboratory.Id.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, renew.StatusCode);
        var renewalId = int.Parse(Regex.Match(renew.Headers.Location!.ToString(), @"\d+$").Value);
        Assert.True(await app.Db(db => db.Solicitudes.AnyAsync(s => s.Id == renewalId && s.EsRenovacion && s.Estado == EstadoSolicitud.Borrador)));
        await Post(owner, "/Gestion/Establecimientos", "/Gestion/Renovar", new() { ["id"] = laboratory.Id.ToString() });
        Assert.Equal(1, await app.Db(db => db.Solicitudes.CountAsync(s => s.LaboratorioId == laboratory.Id && s.EsRenovacion)));
        // Renovación recorre el mismo flujo y conserva el establecimiento.
        await UploadRequirements(owner, renewalId); await Decide(owner, renewalId, "enviar"); await ReviewDocuments(coordinator, renewalId); await Decide(coordinator, renewalId, "aprobar_documentos");
        await Decide(coordinator, renewalId, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = TramiteService.HoraBolivia.AddDays(3).ToString("yyyy-MM-ddTHH:mm") });
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == renewalId); i.Fecha = TramiteService.HoraBolivia.AddMinutes(-1); return await db.SaveChangesAsync(); });
        await Decide(supervisor, renewalId, "acta", new() { ["Resultado"] = "aprobado" }, Pdf); await Decide(coordinator, renewalId, "aprobar_acta"); await Decide(lawyer, renewalId, "aprobar_legal", file: Pdf);
        Assert.Equal(EstadoSolicitud.Habilitado, await State(renewalId));
        Assert.Equal(originalExpiry!.Value.AddYears(1), await app.Db(db => db.Laboratorios.Where(l => l.Id == laboratory.Id).Select(l => l.VigenciaHasta).SingleAsync()));
        Assert.True(await app.Db(db => db.Notificaciones.AnyAsync(n => n.UsuarioId == laboratory.PropietarioId)));
    }

    [Fact]
    public async Task Public_pages_login_errors_and_csrf()
    {
        using var client = app.Browser();
        var labId = await app.Db(async db => {
            var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            var lab = new Laboratorio { Nombre = "Central de prueba pública", Tipo = "Clínico", Estado = EstadoLaboratorio.Abierto,
                Lat = -17.39m, Lng = -66.15m, PropietarioId = owner.Id, DatosVerificados = true, VigenciaHasta = DateTime.UtcNow.AddYears(1) };
            db.Laboratorios.Add(lab); await db.SaveChangesAsync(); return lab.Id;
        });
        foreach (var path in new[] { "/", "/laboratorios", "/laboratorios?search=Central", "/laboratorios?estado=Abierto", $"/laboratorios/Detalle/{labId}", $"/laboratorios/Ruta?laboratorioId={labId}", "/Home/Privacy" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/laboratorios/Detalle/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Gestion")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/auth/Auth/Login", new FormUrlEncodedContent(new Dictionary<string,string> { ["Email"] = "propietario@si-lab.local", ["Password"] = "Demo123!" }))).StatusCode);
        var invalid = await Post(client, "/auth/Auth/Login", "/auth/Auth/Login", new() { ["Email"] = "propietario@si-lab.local", ["Password"] = "incorrecta" });
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("contrase", await invalid.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/uploads/solicitudes/1/documento.pdf")).StatusCode);
    }

    [Fact]
    public async Task Owner_isolation_and_protected_downloads()
    {
        using var owner = await Login("propietario"); var id = await Draft(owner); await UploadRequirements(owner, id);
        await app.Db(async db =>
        {
            if (!await db.Usuarios.AnyAsync(u => u.Email == "otro@si-lab.local"))
            { var u = new Usuario { Nombre = "Otro propietario", Email = "otro@si-lab.local", Rol = RolUsuario.Propietario }; u.Password = new PasswordHasher<Usuario>().HashPassword(u, "Demo123!"); db.Usuarios.Add(u); }
            return await db.SaveChangesAsync();
        });
        using var other = await Login("otro"); using var anonymous = app.Browser();
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/Gestion/Detalle/{id}")).StatusCode);
        var doc = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id).Select(d => d.Id).FirstAsync());
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/Gestion/Descargar/{doc}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/Gestion/Descargar/{doc}")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync($"/Gestion/Descargar/{doc}")).StatusCode);
        using var supervisor = await Login("supervisor");
        Assert.Equal(HttpStatusCode.NotFound, (await supervisor.GetAsync($"/Gestion/Detalle/{id}")).StatusCode);
        var forged = await Post(other, "/solicitud-registro/SolicitudRegistro/Nueva", "/Gestion/Decidir", new() { ["Id"] = id.ToString(), ["Version"] = (await Version(id)).ToString(), ["Accion"] = "enviar" });
        Assert.Equal(HttpStatusCode.NotFound, forged.StatusCode);
    }

    [Fact]
    public async Task Validation_upload_limits_and_stale_changes()
    {
        using var owner = await Login("propietario");
        var invalid = await Post(owner, "/solicitud-registro/SolicitudRegistro/Nueva", "/solicitud-registro/SolicitudRegistro/Nueva", new() { ["NombreLaboratorio"] = "Fuera del mapa", ["TipoLaboratorio"] = "Clínico", ["Municipio"] = "Sacaba", ["Latitud"] = "100", ["Longitud"] = "500" });
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.False(await app.Db(db => db.Solicitudes.AnyAsync(s => s.DatosLaboratorioPropuesto.Contains("Fuera del mapa"))));
        var id = await Draft(owner); var r = await app.Db(db => db.Requisitos.Select(r => r.Id).FirstAsync());
        foreach (var bytes in new[] { Encoding.ASCII.GetBytes("fake PDF"), Encoding.ASCII.GetBytes("%PDF-1.4 incomplete"), new byte[10 * 1024 * 1024 + 1] })
        {
            await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/Documento", new() { ["id"] = id.ToString(), ["requisitoId"] = r.ToString(), ["version"] = "0" }, bytes, "pdf");
            Assert.Equal(0, await app.Db(db => db.Documentos.CountAsync(d => d.SolicitudId == id)));
        }
        await UploadRequirements(owner, id);
        var stale = await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/Decidir", new() { ["Id"] = id.ToString(), ["Version"] = "0", ["Accion"] = "enviar" });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode); Assert.Equal(EstadoSolicitud.Borrador, await State(id));
    }

    [Theory]
    [InlineData("propietario", "/Administracion/Usuarios")]
    [InlineData("supervisor", "/Metricas")]
    [InlineData("gerente", "/Gestion")]
    [InlineData("abogado", "/Administracion/Requisitos")]
    [InlineData("coordinador", "/Gestion/Establecimientos")]
    public async Task Wrong_roles_are_denied(string role, string path)
    {
        using var client = await Login(role); var result = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode); Assert.Contains("AccessDenied", result.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Admin_users_requirements_metrics_and_logout()
    {
        using var admin = await Login("administrador");
        foreach (var path in new[] { "/Administracion/Usuarios", "/Administracion/Roles", "/Administracion/Requisitos", "/Administracion/Usuario", "/Administracion/Requisito", "/Notificaciones" }) Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(path)).StatusCode);
        var created = await Post(admin, "/Administracion/Usuario", "/Administracion/Usuario", new() { ["Nombre"] = "Cuenta prueba", ["Email"] = "gestion-test@si-lab.local", ["Password"] = "PruebaSegura123!", ["Rol"] = "Propietario", ["Activo"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var u = await app.Db(db => db.Usuarios.SingleAsync(u => u.Email == "gestion-test@si-lab.local")); Assert.NotEqual("PruebaSegura123!", u.Password);
        var duplicate = await Post(admin, "/Administracion/Usuario", "/Administracion/Usuario", new() { ["Nombre"] = "Duplicado", ["Email"] = "GESTION-TEST@si-lab.local", ["Password"] = "PruebaSegura123!", ["Rol"] = "Propietario", ["Activo"] = "true" }); Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        var req = await Post(admin, "/Administracion/Requisito", "/Administracion/Requisito", new() { ["Nombre"] = "Requisito opcional prueba", ["Obligatorio"] = "false", ["Activo"] = "true" }); Assert.Equal(HttpStatusCode.Redirect, req.StatusCode);
        var reqId = await app.Db(db => db.Requisitos.Where(r => r.Nombre == "Requisito opcional prueba").Select(r => r.Id).SingleAsync());
        await Post(admin, "/Administracion/Requisitos", "/Administracion/EliminarRequisito", new() { ["id"] = reqId.ToString(), ["version"] = "1" }); Assert.False(await app.Db(db => db.Requisitos.Where(r => r.Id == reqId).Select(r => r.Activo).SingleAsync()));
        using var manager = await Login("gerente"); Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/Metricas")).StatusCode);
        var csv = await manager.GetAsync("/Metricas?descargar=true"); Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType); Assert.Contains("Municipio,Solicitudes", await csv.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.GetAsync("/Metricas?desde=2026-10-12&hasta=2026-01-01")).StatusCode);
        await Post(admin, "/auth/Auth/Dashboard", "/auth/Auth/Logout", new()); Assert.Equal(HttpStatusCode.Redirect, (await admin.GetAsync("/Administracion/Usuarios")).StatusCode);
    }

    [Fact]
    public async Task Expiry_alerts_are_idempotent_and_close_laboratory()
    {
        var id = await app.Db(async db => { var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario); var lab = new Laboratorio { Nombre = "Vencido", Tipo = "Clínico", PropietarioId = owner.Id, VigenciaHasta = DateTime.UtcNow.AddDays(-1), Estado = EstadoLaboratorio.Abierto }; db.Laboratorios.Add(lab); await db.SaveChangesAsync(); return lab.Id; });
        for (var i = 0; i < 2; i++) await app.Db(async db => { await new TramiteService(db).AlertasVigencia(); return 0; });
        Assert.Equal(EstadoLaboratorio.Cerrado, await app.Db(db => db.Laboratorios.Where(l => l.Id == id).Select(l => l.Estado).SingleAsync()));
        Assert.Equal(1, await app.Db(db => db.Notificaciones.CountAsync(n => n.Clave != null && n.Clave.StartsWith($"vigencia:{id}:"))));
        using var client = app.Browser(); Assert.DoesNotContain("Vencido", await client.GetStringAsync("/laboratorios"));
    }

    [Fact]
    public void Business_days_and_malformed_json_are_safe()
    {
        Assert.Equal(new DateTime(2026, 10, 13), TramiteService.DiasHabiles(new DateTime(2026, 10, 9), 2));
        foreach (var json in new[] { "not-json", "[]", "{\"nombre\":123}", "{\"coordenadas\":{\"lat\":\"wrong\"}}" })
        { var vm = new SEDESLABORATORIO.SolicitudSeguimiento.Models.SolicitudSeguimientoItemViewModel { DatosLaboratorioPropuesto = json }; vm.ParseDatosLaboratorio(); Assert.DoesNotContain(json, vm.NombreLaboratorio); }
    }

    [Fact]
    public async Task Migration_preserves_existing_users_and_submitted_requests()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260923042352_AddUniqueEmail");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Usuarios (Nombre,Email,Password,Rol) VALUES ('Anterior','anterior@example.test','hash','propietario')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Solicitudes (PropietarioId,DatosLaboratorioPropuesto,Estado,FechaCreacion) VALUES (1,'{{}}','enrevision','2026-09-01 00:00:00')");
        await migrator.MigrateAsync();
        Assert.True((await db.Usuarios.SingleAsync()).Activo);
        Assert.Equal(1, await db.Solicitudes.CountAsync(s => s.Estado == EstadoSolicitud.EnRevision));
        Assert.False((await db.Solicitudes.SingleAsync()).EsRenovacion);
    }

    [Fact]
    public async Task Changed_role_invalidates_cookie_and_missing_fields_do_not_crash()
    {
        var id = await app.Db(async db => { var u = new Usuario { Nombre = "Revocable", Email = "revocable@si-lab.local", Rol = RolUsuario.Propietario }; u.Password = new PasswordHasher<Usuario>().HashPassword(u, "Demo123!"); db.Usuarios.Add(u); await db.SaveChangesAsync(); return u.Id; });
        using var account = await Login("revocable"); using var admin = await Login("administrador");
        var invalid = await Post(admin, "/Administracion/Usuario", "/Administracion/Usuario", new() { ["Nombre"] = "Incompleto" });
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var changed = await Post(admin, $"/Administracion/Usuario/{id}", "/Administracion/Usuario", new() { ["Id"] = id.ToString(), ["Version"] = "1", ["Nombre"] = "Revocable", ["Email"] = "revocable@si-lab.local", ["Rol"] = "Gerente", ["Activo"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);
        var rejected = await account.GetAsync("/Gestion"); Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode); Assert.Contains("Login", rejected.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Administrator_permissions_are_enforced_on_existing_sessions()
    {
        using var admin = await Login("administrador"); using var owner = await Login("propietario");
        var id = await Draft(owner, "Permisos configurables");
        var change = await Post(admin, "/Administracion/Roles", "/Administracion/Permiso", new() { ["rol"] = "Propietario", ["clave"] = "solicitudes", ["habilitado"] = "false" }); Assert.Equal(HttpStatusCode.Redirect, change.StatusCode);
        try
        {
            var denied = await owner.GetAsync("/solicitud-registro/SolicitudRegistro/Nueva"); Assert.Contains("AccessDenied", denied.Headers.Location!.ToString());
            var lowerCase = await owner.GetAsync("/solicitud-registro/solicitudregistro/nueva"); Assert.Contains("AccessDenied", lowerCase.Headers.Location!.ToString());
            var deniedPost = await Decide(owner, id, "enviar"); Assert.Contains("AccessDenied", deniedPost.Headers.Location!.ToString());
            var escalation = await Post(admin, "/Administracion/Roles", "/Administracion/Permiso", new() { ["rol"] = "Propietario", ["clave"] = "legal", ["habilitado"] = "true" }); Assert.Equal(HttpStatusCode.BadRequest, escalation.StatusCode);
        }
        finally { await Post(admin, "/Administracion/Roles", "/Administracion/Permiso", new() { ["rol"] = "Propietario", ["clave"] = "solicitudes", ["habilitado"] = "true" }); }
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/solicitud-registro/SolicitudRegistro/Nueva")).StatusCode);
    }

    [Fact]
    public async Task Rejections_reassignments_and_agenda_conflicts_preserve_workflow()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador");
        using var supervisor = await Login("supervisor"); using var lawyer = await Login("abogado");
        var supervisorId = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
        var rejectedId = await Draft(owner, "Rechazo documental"); await UploadRequirements(owner, rejectedId); await Decide(owner, rejectedId, "enviar");
        await Decide(coordinator, rejectedId, "rechazar_documentos"); Assert.Equal(EstadoSolicitud.Rechazado, await State(rejectedId));
        await Decide(coordinator, rejectedId, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = TramiteService.HoraBolivia.AddDays(7).ToString("yyyy-MM-ddTHH:mm") });
        Assert.Equal(EstadoSolicitud.Rechazado, await State(rejectedId));
        var id = await Draft(owner, "Reprogramación"); await UploadRequirements(owner, id); await Decide(owner, id, "enviar"); await ReviewDocuments(coordinator, id); await Decide(coordinator, id, "aprobar_documentos");
        var date = TramiteService.HoraBolivia.AddDays(10).ToString("yyyy-MM-ddTHH:mm");
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = date });
        Assert.Equal(EstadoSolicitud.Inspeccion, await State(id));
        var conflictId = await Draft(owner, "Conflicto agenda"); await UploadRequirements(owner, conflictId); await Decide(owner, conflictId, "enviar"); await ReviewDocuments(coordinator, conflictId); await Decide(coordinator, conflictId, "aprobar_documentos");
        await Decide(coordinator, conflictId, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = date }); Assert.Equal(EstadoSolicitud.Aprobado, await State(conflictId));
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = TramiteService.HoraBolivia.AddDays(11).ToString("yyyy-MM-ddTHH:mm") });
        Assert.Equal(1, await app.Db(db => db.Inspecciones.CountAsync(i => i.SolicitudId == id && i.Estado == "pendiente")));
        Assert.Equal(1, await app.Db(db => db.Inspecciones.CountAsync(i => i.SolicitudId == id && i.Estado == "reprogramada")));
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id && i.Estado == "pendiente"); i.Fecha = TramiteService.HoraBolivia.AddMinutes(-1); return await db.SaveChangesAsync(); });
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "rechazado", ["Citacion"] = "Incumplimiento" }, Pdf);
        await Decide(coordinator, id, "rechazar_acta"); Assert.Equal(EstadoSolicitud.Aprobado, await State(id));
        await Decide(coordinator, id, "asignar", new() { ["SupervisorId"] = supervisorId.ToString(), ["Fecha"] = TramiteService.HoraBolivia.AddDays(12).ToString("yyyy-MM-ddTHH:mm") });
        await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id && i.Estado == "pendiente"); i.Fecha = TramiteService.HoraBolivia.AddMinutes(-1); return await db.SaveChangesAsync(); });
        await Decide(supervisor, id, "acta", new() { ["Resultado"] = "aprobado" }, Pdf); await Decide(coordinator, id, "aprobar_acta");
        var docsLegales = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id && d.Actual).Select(d => d.Id).ToListAsync());
        await Decide(lawyer, id, "observar_legal", docsLegales.Select((d,n) => new { Key = $"DocumentosObservados[{n}]", Value = d.ToString() }).ToDictionary(x => x.Key, x => x.Value)); Assert.Equal(EstadoSolicitud.Observado, await State(id));
        Assert.Null(await app.Db(db => db.Solicitudes.Where(s => s.Id == id).Select(s => s.LaboratorioId).SingleAsync()));
        await UploadRequirements(owner, id); await Decide(owner, id, "enviar"); Assert.Equal(EstadoSolicitud.EnRevision, await State(id));
    }
}


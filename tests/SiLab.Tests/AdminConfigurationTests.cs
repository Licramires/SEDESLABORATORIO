using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

// Cada prueba tiene su propia base para no alterar permisos de los demás flujos.
public sealed class AdminConfigurationTests : IDisposable
{
    private readonly TestApplication app = new();
    public void Dispose() => app.Dispose();
    private static async Task<string> Token(HttpClient client, string page = "/Administracion/Requisitos")
    {
        var r = await client.GetAsync(page); Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var m = Regex.Match(await r.Content.ReadAsStringAsync(), "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(m.Success); return WebUtility.HtmlDecode(m.Groups[1].Value);
    }
    private async Task<HttpClient> Login(string role = "administrador")
    {
        var client = app.Browser(); var token = await Token(client, "/auth/Auth/Login");
        var r = await client.PostAsync("/auth/Auth/Login", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["Email"] = role + "@si-lab.local", ["Password"] = "Demo123!", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode); return client;
    }
    private static async Task<HttpResponseMessage> Post(HttpClient c, string action, Dictionary<string,string> fields, string page = "/Administracion/Requisitos")
    {
        fields["__RequestVerificationToken"] = await Token(c, page);
        return await c.PostAsync(action, new FormUrlEncodedContent(fields));
    }
    private async Task<int> Section(HttpClient c, string name = "Legales", int order = 1)
    {
        Assert.Equal(HttpStatusCode.Redirect, (await Post(c, "/Administracion/Seccion", new()
        { ["Nombre"] = name, ["Descripcion"] = "Documentos del personal", ["Orden"] = order.ToString(), ["Activa"] = "true" })).StatusCode);
        return await app.Db(db => db.SeccionesRequisitos.Where(s => s.Nombre == name).Select(s => s.Id).SingleAsync());
    }
    private async Task<int> Requirement(HttpClient c, int section, string name = "Registro profesional", int order = 1)
    {
        Assert.Equal(HttpStatusCode.Redirect, (await Post(c, "/Administracion/Requisito", new()
        { ["Nombre"] = name, ["SeccionId"] = section.ToString(), ["Orden"] = order.ToString(), ["Obligatorio"] = "true", ["Activo"] = "true" })).StatusCode);
        return await app.Db(db => db.Requisitos.Where(r => r.Nombre == name).Select(r => r.Id).SingleAsync());
    }
    private static Dictionary<string,string> CoordinatorPermissions(int version, bool enabled) => new()
    {
        ["Rol"] = "Coordinador", ["Permisos[0].Clave"] = "documentos", ["Permisos[0].Habilitado"] = enabled.ToString(), ["Permisos[0].Version"] = version.ToString(),
        ["Permisos[1].Clave"] = "asignaciones", ["Permisos[1].Habilitado"] = enabled.ToString(), ["Permisos[1].Version"] = version.ToString(),
        ["Permisos[2].Clave"] = "actas", ["Permisos[2].Habilitado"] = "true", ["Permisos[2].Version"] = version.ToString(),
        ["Permisos[3].Clave"] = "informes", ["Permisos[3].Habilitado"] = "true", ["Permisos[3].Version"] = version.ToString()
    };

    [Fact]
    public async Task Bulk_permissions_apply_to_open_sessions_and_reject_stale_edits_atomically()
    {
        using var admin = await Login(); using var coordinator = await Login("coordinador");
        var token = await Token(coordinator, "/auth/Auth/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "/Administracion/EditarPermisos", CoordinatorPermissions(0, false), "/Administracion/Roles")).StatusCode);
        var denied = await coordinator.PostAsync("/Gestion/RevisarDocumento", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["__RequestVerificationToken"] = token, ["Resultado"] = "aprobado", ["Motivo"] = "Prueba" }));
        Assert.Contains("AccessDenied", denied.Headers.Location?.ToString() ?? "");
        Assert.Equal(2, await app.Db(db => db.CambiosPermisos.CountAsync()));
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, "/Administracion/EditarPermisos", CoordinatorPermissions(0, true), "/Administracion/Roles")).StatusCode);
        Assert.Equal(2, await app.Db(db => db.PermisosRol.CountAsync(p => !p.Habilitado)));
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "/Administracion/EditarPermisos", CoordinatorPermissions(1, true), "/Administracion/Roles")).StatusCode);
        Assert.Equal(4, await app.Db(db => db.CambiosPermisos.CountAsync()));
        Assert.Equal(HttpStatusCode.NotFound, (await coordinator.PostAsync("/Gestion/RevisarDocumento", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["__RequestVerificationToken"] = token, ["Id"] = "0", ["Version"] = "0", ["DocumentoId"] = "0", ["Resultado"] = "aprobado", ["Motivo"] = "Prueba" }))).StatusCode);
    }

    [Fact]
    public async Task Permission_editor_rejects_missing_duplicate_and_foreign_capabilities()
    {
        using var admin = await Login();
        foreach(var fields in new[] {
            new Dictionary<string,string> { ["Rol"] = "Coordinador" },
            new Dictionary<string,string>(CoordinatorPermissions(0, false)) { ["Permisos[1].Clave"] = "documentos" },
            new Dictionary<string,string>(CoordinatorPermissions(0, false)) { ["Permisos[0].Clave"] = "usuarios" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Post(admin, "/Administracion/EditarPermisos", fields, "/Administracion/Roles")).StatusCode);
        Assert.False(await app.Db(db => db.PermisosRol.AnyAsync()));
        using var owner = await Login("propietario");
        Assert.Contains("AccessDenied", (await owner.GetAsync("/Administracion/EditarPermisos?rol=Coordinador")).Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Admin_cannot_bypass_disabled_requirements_through_new_section_actions_and_can_recover()
    {
        using var admin = await Login(); var token = await Token(admin, "/Administracion/Roles");
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "/Administracion/Permiso", new() { ["rol"] = "Administrador", ["clave"] = "requisitos", ["habilitado"] = "false" }, "/Administracion/Roles")).StatusCode);
        foreach(var path in new[] { "/Administracion/Requisitos", "/Administracion/Seccion" })
            Assert.Contains("AccessDenied", (await admin.GetAsync(path)).Headers.Location?.ToString() ?? "");
        Assert.Contains("AccessDenied", (await admin.PostAsync("/Administracion/Seccion", new FormUrlEncodedContent(new Dictionary<string,string>
        { ["__RequestVerificationToken"] = token, ["Nombre"] = "No permitida" }))).Headers.Location?.ToString() ?? "");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/Administracion/EditarPermisos?rol=Administrador")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "/Administracion/Permiso", new() { ["rol"] = "Administrador", ["clave"] = "requisitos", ["habilitado"] = "true" }, "/Administracion/Roles")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/Administracion/Requisitos")).StatusCode);
    }

    [Fact]
    public async Task Section_and_requirement_activation_control_public_page_registration_and_drafts()
    {
        using var admin = await Login(); using var owner = await Login("propietario"); using var visitor = app.Browser();
        var section = await Section(admin); var req = await Requirement(admin, section);
        Assert.Contains("Registro profesional", await visitor.GetStringAsync("/publico/Publico/Requerimientos"));
        Assert.Contains("Legales", await owner.GetStringAsync("/solicitud-registro/SolicitudRegistro/Nueva"));
        await Post(admin, "/Administracion/EstadoSeccion", new() { ["id"] = section.ToString(), ["version"] = "1", ["activa"] = "false" });
        Assert.DoesNotContain("Registro profesional", await visitor.GetStringAsync("/publico/Publico/Requerimientos"));
        Assert.DoesNotContain("Registro profesional", await owner.GetStringAsync("/solicitud-registro/SolicitudRegistro/Nueva"));
        Assert.True(await app.Db(db => db.Requisitos.Where(r => r.Id == req).Select(r => r.Activo).SingleAsync()));
        Assert.DoesNotContain(await app.Db(db => ExpedienteService.Requisitos(db, new Solicitud())), r => r.Id == req);
        await Post(admin, "/Administracion/EstadoRequisito", new() { ["id"] = req.ToString(), ["version"] = "1", ["activo"] = "false" });
        await Post(admin, "/Administracion/EstadoSeccion", new() { ["id"] = section.ToString(), ["version"] = "2", ["activa"] = "true" });
        Assert.DoesNotContain("Registro profesional", await visitor.GetStringAsync("/publico/Publico/Requerimientos"));
        await Post(admin, "/Administracion/EstadoRequisito", new() { ["id"] = req.ToString(), ["version"] = "2", ["activo"] = "true" });
        Assert.Contains("Registro profesional", await visitor.GetStringAsync("/publico/Publico/Requerimientos"));
    }

    [Fact]
    public async Task Deleting_requirements_and_empty_sections_preserves_submitted_snapshots()
    {
        using var admin = await Login(); var section = await Section(admin); var req = await Requirement(admin, section);
        var old = await app.Db(db => ExpedienteService.Requisitos(db, new Solicitud()));
        var snapshot = JsonSerializer.Serialize(old);
        Assert.Equal("Legales", old.Single(r => r.Id == req).SeccionNombre);
        await Post(admin, "/Administracion/EliminarSeccion", new() { ["id"] = section.ToString(), ["version"] = "1" });
        Assert.False(await app.Db(db => db.SeccionesRequisitos.Where(s => s.Id == section).Select(s => s.Eliminada).SingleAsync()));
        await Post(admin, "/Administracion/EliminarRequisito", new() { ["id"] = req.ToString(), ["version"] = "1" });
        Assert.True(await app.Db(db => db.Requisitos.Where(r => r.Id == req).Select(r => r.Eliminado).SingleAsync()));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/Administracion/Requisito/{req}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(admin, "/Administracion/EstadoRequisito", new() { ["id"] = req.ToString(), ["version"] = "2", ["activo"] = "true" })).StatusCode);
        await Post(admin, "/Administracion/EliminarSeccion", new() { ["id"] = section.ToString(), ["version"] = "1" });
        Assert.True(await app.Db(db => db.SeccionesRequisitos.Where(s => s.Id == section).Select(s => s.Eliminada).SingleAsync()));
        var frozen = await app.Db(db => ExpedienteService.Requisitos(db, new Solicitud { RequisitosJson = snapshot }));
        Assert.Equal(old, frozen);
        Assert.DoesNotContain("Registro profesional", await admin.GetStringAsync("/Administracion/Requisitos"));
        Assert.DoesNotContain(await app.Db(db => ExpedienteService.Requisitos(db, new Solicitud())), r => r.Id == req);
    }

    [Fact]
    public async Task Edits_reorder_move_validate_sections_and_detect_stale_catalog_changes()
    {
        using var admin = await Login(); using var visitor = app.Browser();
        var a = await Section(admin, "Alfa", 20); var b = await Section(admin, "Beta", 10);
        var req = await Requirement(admin, a, "RequisitoZ", 9); await Requirement(admin, a, "RequisitoA", 1);
        await Requirement(admin, b, "RequisitoB");
        var html = await visitor.GetStringAsync("/publico/Publico/Requerimientos");
        Assert.True(html.IndexOf("Beta", StringComparison.Ordinal) < html.IndexOf("Alfa", StringComparison.Ordinal));
        Assert.True(html.IndexOf("RequisitoA", StringComparison.Ordinal) < html.IndexOf("RequisitoZ", StringComparison.Ordinal));
        var edit = new Dictionary<string,string> { ["Id"] = req.ToString(), ["Version"] = "1", ["Nombre"] = "Movido", ["Obligatorio"] = "false", ["SeccionId"] = b.ToString(), ["Orden"] = "0", ["Activo"] = "true" };
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "/Administracion/Requisito", edit)).StatusCode);
        Assert.Equal(b, await app.Db(db => db.Requisitos.Where(r => r.Id == req).Select(r => r.SeccionId).SingleAsync()));
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, "/Administracion/Requisito", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, "/Administracion/EliminarRequisito", new() { ["id"] = req.ToString(), ["version"] = "1" })).StatusCode);
        edit["Version"] = "2"; edit["SeccionId"] = "99999";
        Assert.Contains("Seleccione una", WebUtility.HtmlDecode(await (await Post(admin, "/Administracion/Requisito", edit)).Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "/Administracion/Seccion", new() { ["Id"] = a.ToString(), ["Version"] = "1", ["Nombre"] = "Alfa editada", ["Activa"] = "true", ["Orden"] = "0" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Post(admin, "/Administracion/EstadoSeccion", new() { ["id"] = a.ToString(), ["version"] = "1", ["activa"] = "false" })).StatusCode);
    }

    [Fact]
    public async Task Upgrade_preserves_existing_requirements_permissions_and_legacy_snapshot_json()
    {
        await using var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options);
        var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync("20261018233307_DocumentOriginalName");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Requisitos (Nombre,Obligatorio,Activo,Version,PlantillaPdf) VALUES ('Anterior',1,1,3,'plantilla.pdf')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO PermisosRol (Rol,Clave,Habilitado) VALUES ('Coordinador','documentos',0)");
        await migrator.MigrateAsync();
        var r = await db.Requisitos.SingleAsync(); Assert.Equal(3, r.Version); Assert.Equal("plantilla.pdf", r.PlantillaPdf);
        Assert.Null(r.SeccionId); Assert.False(r.Eliminado); Assert.True(r.Activo);
        Assert.False((await db.PermisosRol.SingleAsync()).Habilitado);
        var copia = Assert.Single(ExpedienteService.LeerRequisitos(new Solicitud { RequisitosJson = "[{\"Id\":1,\"Nombre\":\"Anterior\",\"Obligatorio\":true,\"Version\":3,\"PlantillaPdf\":\"plantilla.pdf\"}]" }));
        Assert.Equal("Anterior", copia.Nombre); Assert.Null(copia.SeccionId);
    }
}

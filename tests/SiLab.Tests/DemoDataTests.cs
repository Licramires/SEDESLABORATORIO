using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public class DemoDataTests
{
    private static async Task Initialize(TestApplication app)
    {
        var config = new ConfigurationBuilder().AddConfiguration(app.Services.GetRequiredService<IConfiguration>())
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:SeedDemoData"] = "true" }).Build();
        await DatabaseInitializer.InitializeAsync(app.Services, app.Services.GetRequiredService<IWebHostEnvironment>(), config);
    }

    [Fact]
    public async Task Demo_has_consistent_private_documents_tasks_for_all_roles_and_no_duplicates_on_restart()
    {
        using var app = new TestApplication { SeedDemoData = true };
        await using var scope = app.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<PdfStorage>();
        var original = await app.Db(async db => {
            var cases = await db.Solicitudes.Include(s => s.Documentos).Include(s => s.Inspecciones)
                .Include(s => s.Historial).Include(s => s.Citaciones).ToListAsync();
            Assert.Equal(13, cases.Count);
            foreach (var s in cases)
            {
                Assert.StartsWith("DEMO-", s.Codigo);
                ExpedienteService.ValidarDatos(s);
                Assert.NotEmpty(s.Historial);
                if (s.Codigo != "DEMO-OBSERVADO")
                    ExpedienteService.ComprobarDocumentos(s, await ExpedienteService.Requisitos(db, s), storage);
                else Assert.Throws<InvalidOperationException>(() =>
                    ExpedienteService.ComprobarDocumentos(s, ExpedienteService.LeerRequisitos(s), storage));
                foreach (var d in s.Documentos) Assert.NotNull(storage.Resolve(d.ArchivoPdf));
                foreach (var i in s.Inspecciones.Where(i => i.Estado != "pendiente"))
                {
                    Assert.NotNull(storage.Resolve(i.ActaPdf));
                    Assert.NotEmpty(ExpedienteService.LeerVerificaciones(i));
                }
            }
            var legal = cases.Single(s => s.Codigo == "DEMO-LEGAL");
            ExpedienteService.ComprobarDocumentos(legal, ExpedienteService.LeerRequisitos(legal), storage, revisados: true);
            Assert.Equal("validada", Assert.Single(legal.Inspecciones).Estado);
            var citation = Assert.Single(cases.Single(s => s.Codigo == "DEMO-CITACION").Citaciones);
            Assert.NotNull(storage.Resolve(citation.EvidenciaPdf));
            Assert.Equal(citation.InspeccionId, cases.Single(s => s.Codigo == "DEMO-CITACION").Inspecciones.Single().Id);
            var labs = await db.Laboratorios.Where(l => l.Codigo.StartsWith("DEMO-LAB-")).ToListAsync();
            Assert.Equal(2, labs.Count);
            Assert.All(labs, l => { Assert.True(l.DatosVerificados); Assert.NotNull(l.PropietarioId); Assert.NotNull(l.VigenciaHasta); });
            Assert.Equal(6, await db.Notificaciones.CountAsync(n => n.Clave != null && n.Clave.StartsWith("demo:expedientes:v1")));
            var closed = cases.Single(s => s.Estado == EstadoSolicitud.Habilitado);
            Assert.NotNull(closed.InformeCierrePdf);
            var draft = cases.Single(s => s.Estado == EstadoSolicitud.Borrador);
            draft.Telefono = "avance conservado"; draft.Version++;
            await db.SaveChangesAsync();
            return (Cases: cases.Count, Docs: await db.Documentos.CountAsync(), Catalogs: await db.Catalogos.CountAsync(),
                History: await db.HistorialTramites.CountAsync(), Notifications: await db.Notificaciones.CountAsync());
        });
        var files = Directory.GetFiles(storage.Root).Order().ToArray();
        await Initialize(app);
        await app.Db(async db => {
            Assert.Equal(original.Cases, await db.Solicitudes.CountAsync());
            Assert.Equal(original.Docs, await db.Documentos.CountAsync());
            Assert.Equal(original.Catalogs, await db.Catalogos.CountAsync());
            Assert.Equal(original.History, await db.HistorialTramites.CountAsync());
            Assert.Equal(original.Notifications, await db.Notificaciones.CountAsync());
            Assert.Equal("avance conservado", (await db.Solicitudes.SingleAsync(s => s.Codigo == "DEMO-BORRADOR")).Telefono);
            return 0;
        });
        Assert.Equal(files, Directory.GetFiles(storage.Root).Order().ToArray());

        foreach (var (role, page) in new[] { ("propietario", "/Gestion/Index"), ("coordinador", "/Gestion/Index"),
            ("supervisor", "/Gestion/Agenda"), ("abogado", "/Gestion/Index"), ("gerente", "/Metricas"), ("administrador", "/Administracion/Catalogos") })
        {
            using var client = app.Browser();
            var login = await client.GetStringAsync("/auth/Auth/Login");
            var token = WebUtility.HtmlDecode(Regex.Match(login, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            var response = await client.PostAsync("/auth/Auth/Login", new FormUrlEncodedContent(new Dictionary<string,string> {
                ["Email"] = role + "@si-lab.local", ["Password"] = "Demo123!", ["__RequestVerificationToken"] = token }));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var html = await client.GetAsync(page);
            Assert.Equal(HttpStatusCode.OK, html.StatusCode);
            Assert.Contains("<main", await html.Content.ReadAsStringAsync());
            if (role == "abogado")
            {
                var legal = await app.Db(db => db.Solicitudes.SingleAsync(s => s.Codigo == "DEMO-LEGAL"));
                var detail = await client.GetStringAsync($"/Gestion/Detalle/{legal.Id}");
                var csrf = WebUtility.HtmlDecode(Regex.Match(detail, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
                using var form = new MultipartFormDataContent();
                foreach (var pair in new Dictionary<string,string> { ["Id"] = legal.Id.ToString(), ["Version"] = legal.Version.ToString(),
                    ["Accion"] = "aprobar_legal", ["Motivo"] = "Revisión de prueba completa", ["__RequestVerificationToken"] = csrf })
                    form.Add(new StringContent(pair.Value), pair.Key);
                form.Add(new ByteArrayContent(SimplePdf.Create(["DEMO: aprobación legal ficticia"])), "Pdf", "firma-demo.pdf");
                Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Gestion/Decidir", form)).StatusCode);
                await app.Db(async db => {
                    var closed = await db.Solicitudes.SingleAsync(s => s.Id == legal.Id);
                    Assert.Equal(EstadoSolicitud.Habilitado, closed.Estado);
                    Assert.NotNull(closed.LaboratorioId); Assert.NotNull(closed.InformeCierrePdf);
                    return 0;
                });
            }
        }
    }

    [Fact]
    public async Task Existing_data_catalogs_and_credentials_are_preserved_while_missing_demo_users_are_created()
    {
        using var app = new TestApplication();
        var saved = await app.Db(async db => {
            var user = await db.Usuarios.SingleAsync(u => u.Email == "propietario@si-lab.local");
            user.Nombre = "Cuenta conservada";
            var password = user.Password;
            db.Usuarios.Remove(await db.Usuarios.SingleAsync(u => u.Email == "abogado@si-lab.local"));
            db.Catalogos.Add(new CatalogoItem { Clase = "tipo", Nombre = "Tipo existente" });
            db.Catalogos.Add(new CatalogoItem { Clase = "municipio", Nombre = "Municipio existente" });
            db.Solicitudes.Add(new Solicitud { PropietarioId = user.Id, Codigo = "EXISTENTE", Estado = EstadoSolicitud.Borrador,
                DatosLaboratorioPropuesto = "{}", FechaCreacion = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return password;
        });
        await Initialize(app);
        await app.Db(async db => {
            Assert.Equal(6, await db.Usuarios.CountAsync());
            var owner = await db.Usuarios.SingleAsync(u => u.Email == "propietario@si-lab.local");
            Assert.Equal("Cuenta conservada", owner.Nombre); Assert.Equal(saved, owner.Password);
            Assert.Single(await db.Solicitudes.Where(s => s.Codigo == "EXISTENTE").ToListAsync());
            Assert.Equal("Tipo existente", Assert.Single(await db.Catalogos.Where(c => c.Clase == "tipo").ToListAsync()).Nombre);
            Assert.Equal(13, await db.Solicitudes.CountAsync(s => s.Codigo.StartsWith("DEMO-")));
            Assert.All(await db.Solicitudes.Where(s => s.Codigo.StartsWith("DEMO-")).ToListAsync(),
                s => Assert.Equal("Municipio existente", s.Municipio));
            return 0;
        });
    }
}

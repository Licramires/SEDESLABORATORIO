using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SEDESLABORATORIO.Application.Abstractions;
using SEDESLABORATORIO.Application.Metricas;
using SEDESLABORATORIO.Application.Tramites;
using SEDESLABORATORIO.Auth.Services;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;
using Xunit;
using System.Net;
using System.Text.RegularExpressions;

namespace SiLab.Tests;

public sealed class SolidContractsTests
{
    private sealed class AccionAdicional(bool fallar = false, IAlmacenPdf? archivos = null) : IAccionTramite
    {
        public IReadOnlyCollection<string> Acciones => ["accion_de_extension"];
        public string Permiso => "documentos";
        public async Task<string> Ejecutar(ContextoDecision contexto)
        {
            contexto.ExigirRol("coordinador");
            contexto.ExigirEstado(EstadoSolicitud.EnRevision);
            contexto.Solicitud.Municipio = "Municipio actualizado por extensión";
            if (archivos is not null)
            {
                var bytes = SimplePdf.Create(["Archivo transaccional de ensayo"]);
                await using var input = new MemoryStream(bytes);
                contexto.ArchivoNuevo = await archivos.SaveAsync(new FormFile(input, 0, bytes.Length, "Pdf", "ensayo.pdf"));
            }
            if (fallar) throw new InvalidOperationException("Fallo controlado de extensión");
            return "Extensión ejecutada sin modificar el despachador";
        }
    }

    [Fact]
    public void Registry_accepts_an_extension_and_rejects_duplicate_keys()
    {
        var extension = new AccionAdicional();
        var registry = new RegistroAccionesTramite([extension]);
        Assert.Same(extension, registry.Buscar("accion_de_extension"));
        Assert.Null(registry.Buscar("inexistente"));
        Assert.Null(registry.Buscar(null));
        Assert.Null(registry.Buscar(""));
        Assert.Throws<InvalidOperationException>(() => new RegistroAccionesTramite([extension, new AccionAdicional()]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Additional_handler_uses_the_same_commit_rollback_and_file_contract(bool fallar)
    {
        using var app = new TestApplication();
        await app.Db(async db =>
        {
            var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            var coordinator = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Coordinador);
            var solicitud = new Solicitud { PropietarioId = owner.Id, Estado = EstadoSolicitud.EnRevision, Municipio = "Anterior", Version = 1 };
            db.Solicitudes.Add(solicitud); await db.SaveChangesAsync();
            await using var scope = app.Services.CreateAsyncScope();
            var files = scope.ServiceProvider.GetRequiredService<IAlmacenPdf>();
            var before = Directory.Exists(files.Root) ? Directory.GetFiles(files.Root).Order().ToArray() : [];
            var service = new DecisionesTramite(db, new VisibilidadTramites(db),
                new RegistroAccionesTramite([new AccionAdicional(fallar, files)]), new RegistroTramite(db), files, new InformeCierre());
            var result = await service.Ejecutar(new(coordinator.Id, "coordinador"), new() { Id = solicitud.Id, Version = 1, Accion = "accion_de_extension" });
            var saved = await db.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitud.Id);
            Assert.Equal(fallar ? EstadoDecision.NoPermitida : EstadoDecision.Correcta, result.Estado);
            Assert.Equal(fallar ? "Anterior" : "Municipio actualizado por extensión", saved.Municipio);
            Assert.Equal(fallar ? 1 : 2, saved.Version);
            Assert.Equal(fallar ? 0 : 1, await db.HistorialTramites.CountAsync(h => h.SolicitudId == solicitud.Id));
            Assert.Equal(fallar ? 0 : 1, await db.Notificaciones.CountAsync(n => n.UsuarioId == owner.Id && n.Mensaje.Contains("Extensión ejecutada")));
            var after = Directory.GetFiles(files.Root).Order().ToArray();
            if (fallar) Assert.Equal(before, after);
            else Assert.Single(after.Except(before));
            return 0;
        });
    }

    [Theory]
    [InlineData("propietario", EstadoSolicitud.EnRevision)]
    [InlineData("coordinador", EstadoSolicitud.Habilitado)]
    public async Task Extension_cannot_mutate_before_role_and_state_preconditions(string role, EstadoSolicitud state)
    {
        var solicitud = new Solicitud { Estado = state, Municipio = "Anterior" };
        var context = new ContextoDecision(new(1, role), solicitud, new());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new AccionAdicional().Ejecutar(context));
        Assert.Equal("Anterior", solicitud.Municipio);
    }

    [Fact]
    public void Data_ports_resolve_to_one_scoped_unit_of_work_and_keep_read_ports_small()
    {
        using var app = new TestApplication();
        using var scope = app.Services.CreateScope(); var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ApplicationDbContext>();
        Assert.Same(db, services.GetRequiredService<IGestionDatos>());
        Assert.Same(db, services.GetRequiredService<IRegistroDatos>());
        Assert.Same(db, services.GetRequiredService<IAutenticacionDatos>());
        Assert.Same(db, services.GetRequiredService<IMetricasDatos>());
        Assert.DoesNotContain(typeof(IConfirmacionDatos), typeof(IMetricasDatos).GetInterfaces());
        Assert.DoesNotContain(typeof(IUnidadTrabajo), typeof(IPublicoDatos).GetInterfaces());
        Assert.False(typeof(IAlmacenPdf).IsAssignableFrom(typeof(ILectorArchivos)));
        var registry = services.GetRequiredService<RegistroAccionesTramite>();
        var actions = services.GetServices<IAccionTramite>().ToArray();
        Assert.NotEmpty(actions);
        Assert.All(actions, handler => Assert.All(handler.Acciones, key => Assert.Same(handler, registry.Buscar(key))));
    }

    private sealed class HasherAlternativo : IPasswordHasher<Usuario>
    {
        public int Comprobaciones { get; private set; }
        public string HashPassword(Usuario user, string password) => "credencial-renovada-de-ensayo";
        public PasswordVerificationResult VerifyHashedPassword(Usuario user, string hashedPassword, string providedPassword)
        {
            Comprobaciones++;
            return providedPassword == "entrada-valida" ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Failed;
        }
    }

    [Theory]
    [InlineData("entrada-valida", true)]
    [InlineData("entrada-incorrecta", false)]
    public async Task Authentication_honors_a_substituted_hasher_and_preserves_rehash_contract(string password, bool valid)
    {
        using var app = new TestApplication();
        await app.Db(async db =>
        {
            var user = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            var previous = user.Password; var hasher = new HasherAlternativo();
            IAuthService auth = new AuthService(db, hasher);
            var result = await auth.ValidateCredentialsAsync("  " + user.Email.ToUpperInvariant() + "  ", password);
            Assert.Equal(valid, result.Succeeded); Assert.Equal(1, hasher.Comprobaciones);
            Assert.Equal(valid ? "credencial-renovada-de-ensayo" : previous, (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == user.Id)).Password);
            return 0;
        });
    }

    private sealed class RelojFijo : IReloj
    {
        public DateTime UtcNow => new(2040, 1, 2, 2, 30, 0, DateTimeKind.Utc);
        public DateTime HoraBolivia => UtcNow.AddHours(-4);
    }

    [Fact]
    public async Task Agenda_and_metrics_use_a_substituted_clock_without_changing_the_machine()
    {
        using var app = new TestApplication();
        await app.Db(async db =>
        {
            var clock = new RelojFijo();
            var supervisor = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Supervisor);
            var agenda = await new ConsultaAgenda(db, clock).Consultar(supervisor.Id, null, false);
            Assert.Equal("2040-01-01", agenda.Hoy); Assert.Equal("2040-01-01T22:30", agenda.Ahora);
            var metrics = await new ConsultaMetricas(db, clock).Consultar(new(2040, 1, 1), new(2040, 1, 31), null);
            Assert.Equal(clock.UtcNow, metrics.Visuales.ActualizadoUtc);
            Assert.Empty(metrics.Filas); Assert.Null(metrics.Visuales.TasaAprobacion);
            return 0;
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_empty_action_is_a_validation_error_not_a_dispatch_failure(bool includeEmpty)
    {
        using var app = new TestApplication();
        var id = await app.Db(async db =>
        {
            var owner = await db.Usuarios.FirstAsync(u => u.Rol == RolUsuario.Propietario);
            var request = new Solicitud { PropietarioId = owner.Id, Estado = EstadoSolicitud.Borrador, Version = 1 };
            db.Solicitudes.Add(request); await db.SaveChangesAsync(); return request.Id;
        });
        using var client = app.Browser();
        static string Token(string page) => WebUtility.HtmlDecode(Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var token = Token(await client.GetStringAsync("/auth/Auth/Login"));
        var login = await client.PostAsync("/auth/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = "propietario@si-lab.local", ["Password"] = "Demo123!", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        token = Token(await client.GetStringAsync("/auth/Auth/Dashboard"));
        var fields = new Dictionary<string, string> { ["Id"] = id.ToString(), ["Version"] = "1", ["__RequestVerificationToken"] = token };
        if (includeEmpty) fields["Accion"] = "";
        var response = await client.PostAsync("/Gestion/Decidir", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Detalle/" + id, response.Headers.Location?.ToString());
        await app.Db(async db =>
        {
            var request = await db.Solicitudes.FindAsync(id);
            Assert.Equal(1, request!.Version); Assert.Equal(EstadoSolicitud.Borrador, request.Estado);
            Assert.False(await db.HistorialTramites.AnyAsync(h => h.SolicitudId == id)); return 0;
        });
    }
}

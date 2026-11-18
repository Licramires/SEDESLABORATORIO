using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Infrastructure;

// Fachada compatible con consumidores anteriores. El flujo de producción usa
// los contratos separados; aquí no se duplican reglas ni se confirman cambios.
public sealed class TramiteService(ApplicationDbContext db, IConfiguration? configuration = null)
{
    public static DateTime HoraBolivia => DateTime.UtcNow.AddHours(-4);
    public static DateTime DiasHabiles(DateTime fecha, int dias)
    {
        while (dias > 0) { fecha = fecha.AddDays(1); if (fecha.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) dias--; }
        return fecha.Date;
    }
    public Task<DateTime> PlazoHabil(DateTime fecha, int dias) => new CalendarioLaboral(db).PlazoHabil(fecha, dias);
    public void Registrar(Solicitud solicitud, int actorId, string detalle) => new RegistroTramite(db).Registrar(solicitud, actorId, detalle);
    public Task AvisarRol(RolUsuario rol, string mensaje) => new AvisosRol(db).AvisarRol(rol, mensaje);
    public Task AlertasVigencia() => new AlertasVigenciaService(db, configuration, new RelojSistema()).AlertasVigencia();
}

public sealed class VigenciaWorker(IServiceScopeFactory scopes, ILogger<VigenciaWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try { await using var scope = scopes.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<IAlertasVigencia>().AlertasVigencia(); }
            catch (Exception ex) { logger.LogError(ex, "No se pudieron procesar las alertas de vigencia."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

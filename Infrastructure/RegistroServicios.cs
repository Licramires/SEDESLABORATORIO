using SEDESLABORATORIO.Application.Tramites;
using SEDESLABORATORIO.Application.Metricas;
using Microsoft.AspNetCore.Identity;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Data;

namespace SEDESLABORATORIO.Infrastructure;

public static class RegistroServicios
{
    public static IServiceCollection AddSiLabApplication(this IServiceCollection services)
    {
        // Todos los puertos de datos comparten el contexto scoped ya registrado.
        services.AddScoped<IAdministracionDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGestionDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IRegistroDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IRequisitosDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICatalogosDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ISolicitudesDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutenticacionDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPublicoDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IMetricasDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAgendaDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDecisionesDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAsignacionesDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IHabilitacionDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<INotificacionesDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPermisosDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IRegistroHistorialDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAvisosDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAlertasDatos>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAlmacenPdf>(sp => sp.GetRequiredService<PdfStorage>());
        services.AddScoped<ILectorArchivos>(sp => sp.GetRequiredService<PdfStorage>());
        services.AddSingleton<IReloj, RelojSistema>();
        services.AddScoped<IRegistroTramite, RegistroTramite>();
        services.AddScoped<IAvisosRol, AvisosRol>();
        services.AddScoped<ICalendarioLaboral, CalendarioLaboral>();
        services.AddScoped<IAlertasVigencia, AlertasVigenciaService>();
        services.AddScoped<IAgendaReservas, AgendaReservas>();
        services.AddScoped<IVisibilidadTramites, VisibilidadTramites>();
        services.AddScoped<IInformeCierre, InformeCierre>();
        services.AddScoped<IAccionTramite, EnviarSolicitud>();
        services.AddScoped<IAccionTramite, RevisarDocumentacion>();
        services.AddScoped<IAccionTramite, AsignarInspeccion>();
        services.AddScoped<IAccionTramite, ProgramarInspeccion>();
        services.AddScoped<IAccionTramite, RegistrarActa>();
        services.AddScoped<IAccionTramite, ValidarActa>();
        services.AddScoped<IAccionTramite, ResolverLegal>();
        services.AddScoped<RegistroAccionesTramite>();
        services.AddScoped<IDecisionesTramite, DecisionesTramite>();
        services.AddScoped<IConsultaAgenda, ConsultaAgenda>();
        services.AddScoped<IConsultaMetricas, ConsultaMetricas>();
        services.AddScoped<IExportadorMetricas, ExportadorMetricasCsv>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        return services;
    }
}

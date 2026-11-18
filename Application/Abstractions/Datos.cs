using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Application.Abstractions;

// Puertos por consumidor. EF Core sigue siendo la API de consultas elegida;
// ninguna política necesita crear o localizar el contexto concreto.
public interface ITransaccionDatos : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IConfirmacionDatos
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
public interface IUnidadTrabajo : IConfirmacionDatos
{
    Task<ITransaccionDatos> BeginTransactionAsync(CancellationToken cancellationToken = default);
    void DescartarCambios();
}

public interface IRequisitosDatos { DbSet<Requisito> Requisitos { get; } }
public interface ICatalogosDatos { DbSet<CatalogoItem> Catalogos { get; } }
public interface IAutenticacionDatos : IConfirmacionDatos
{
    DbSet<Usuario> Usuarios { get; }
}
public interface IPublicoDatos : IRequisitosDatos { DbSet<Laboratorio> Laboratorios { get; } }
public interface ISolicitudesDatos
{
    DbSet<Solicitud> Solicitudes { get; }
}
public interface IRegistroDatos : IUnidadTrabajo, IRequisitosDatos, ICatalogosDatos, ISolicitudesDatos { }
public interface IAgendaDatos { DbSet<Inspeccion> Inspecciones { get; } }
public interface IMetricasDatos : ISolicitudesDatos, IAgendaDatos
{
    DbSet<Documento> Documentos { get; }
    DbSet<HistorialTramite> HistorialTramites { get; }
}
public interface INotificacionesDatos : IConfirmacionDatos
{
    DbSet<Notificacion> Notificaciones { get; }
}
public interface IGestionDatos : IRegistroDatos, IMetricasDatos, INotificacionesDatos
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Laboratorio> Laboratorios { get; }
    DbSet<Citacion> Citaciones { get; }
}
public interface IAdministracionDatos : IUnidadTrabajo, IRequisitosDatos, ICatalogosDatos, ISolicitudesDatos, IAgendaDatos
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Laboratorio> Laboratorios { get; }
    DbSet<Documento> Documentos { get; }
    DbSet<SeccionRequisito> SeccionesRequisitos { get; }
    DbSet<PermisoRol> PermisosRol { get; }
    DbSet<CambioPermiso> CambiosPermisos { get; }
}
public interface IPermisosDatos { DbSet<PermisoRol> PermisosRol { get; } }
public interface IDecisionesDatos : IUnidadTrabajo, ISolicitudesDatos
{
    DbSet<HistorialTramite> HistorialTramites { get; }
}
public interface IAsignacionesDatos : ISolicitudesDatos, ICatalogosDatos, IRequisitosDatos
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Notificacion> Notificaciones { get; }
}
public interface IHabilitacionDatos : IRequisitosDatos
{
    DbSet<Laboratorio> Laboratorios { get; }
}
public interface IRegistroHistorialDatos
{
    DbSet<HistorialTramite> HistorialTramites { get; }
    DbSet<Notificacion> Notificaciones { get; }
}
public interface IAvisosDatos
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Notificacion> Notificaciones { get; }
}
public interface IAlertasDatos : IUnidadTrabajo
{
    DbSet<Laboratorio> Laboratorios { get; }
    DbSet<Notificacion> Notificaciones { get; }
    DbSet<Citacion> Citaciones { get; }
    DbSet<Inspeccion> Inspecciones { get; }
}

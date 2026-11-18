using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Application.Abstractions;

public interface IRegistroTramite { void Registrar(Solicitud solicitud, int actorId, string detalle); }
public interface IAvisosRol { Task AvisarRol(RolUsuario rol, string mensaje); }
public interface ICalendarioLaboral { Task<DateTime> PlazoHabil(DateTime fecha, int dias); }
public interface IAlertasVigencia { Task AlertasVigencia(); }
public interface IAgendaReservas { Task<bool> HayConflicto(int supervisorId, DateTime inicio, DateTime fin, int solicitudId); }
public interface IVisibilidadTramites { IQueryable<Solicitud> Visibles(int actorId, string rol); }

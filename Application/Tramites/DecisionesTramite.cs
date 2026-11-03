using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public interface IInformeCierre
{
    byte[] Crear(Solicitud solicitud, IEnumerable<HistorialTramite> historial);
}

public sealed class InformeCierre : IInformeCierre
{
    public byte[] Crear(Solicitud solicitud, IEnumerable<HistorialTramite> historial) => InformeTramite.Crear(solicitud, historial);
}

// Resuelve extensiones por contrato; ni el controlador ni el coordinador tienen
// que añadir ramas cuando se registra una nueva acción.
public sealed class RegistroAccionesTramite
{
    private readonly IReadOnlyDictionary<string, IAccionTramite> acciones;
    public RegistroAccionesTramite(IEnumerable<IAccionTramite> manejadores)
    {
        var mapa = new Dictionary<string, IAccionTramite>(StringComparer.Ordinal);
        foreach (var manejador in manejadores)
        {
            if (manejador.Acciones.Count == 0) throw new InvalidOperationException("Un manejador debe declarar sus acciones.");
            if (string.IsNullOrWhiteSpace(manejador.Permiso)) throw new InvalidOperationException("Un manejador debe declarar su permiso.");
            foreach (var accion in manejador.Acciones)
                if (string.IsNullOrWhiteSpace(accion) || !mapa.TryAdd(accion, manejador))
                    throw new InvalidOperationException($"Acción de trámite vacía o duplicada: {accion}.");
        }
        acciones = mapa;
    }
    public IAccionTramite? Buscar(string? accion) => accion is null ? null : acciones.GetValueOrDefault(accion);
}

public sealed class DecisionesTramite(IDecisionesDatos db, IVisibilidadTramites visibilidad,
    RegistroAccionesTramite acciones, IRegistroTramite historial, IAlmacenPdf archivos, IInformeCierre informes) : IDecisionesTramite
{
    public async Task<ResultadoDecision> Ejecutar(ActorTramite actor, DecisionModel modelo)
    {
        await using var transaction = await db.BeginTransactionAsync();
        var solicitud = await visibilidad.Visibles(actor.Id, actor.Rol).AsSplitQuery()
            .Include(s => s.Documentos).Include(s => s.Inspecciones).Include(s => s.Citaciones)
            .Include(s => s.Laboratorio).SingleOrDefaultAsync(s => s.Id == modelo.Id);
        if (solicitud is null) return new(EstadoDecision.Inexistente);
        if (solicitud.Version != modelo.Version) return new(EstadoDecision.Conflicto, "El trámite cambió. Recargue la página antes de continuar.");
        var accion = acciones.Buscar(modelo.Accion);
        if (accion is null) return new(EstadoDecision.Invalida, "Acción inválida.");
        var contexto = new ContextoDecision(actor, solicitud, modelo);
        try
        {
            var detalle = await accion.Ejecutar(contexto);
            historial.Registrar(solicitud, actor.Id, detalle);
            await db.SaveChangesAsync();
            if (solicitud.Estado == EstadoSolicitud.Habilitado)
            {
                var movimientos = await db.HistorialTramites.Where(h => h.SolicitudId == solicitud.Id)
                    .Include(h => h.Actor).OrderBy(h => h.Fecha).ThenBy(h => h.Id).ToListAsync();
                solicitud.InformeCierrePdf = informes.Crear(solicitud, movimientos);
                await db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return new(EstadoDecision.Correcta, detalle);
        }
        catch (InvalidOperationException ex)
        {
            if (contexto.ArchivoNuevo is not null) archivos.Delete(contexto.ArchivoNuevo);
            await transaction.RollbackAsync();
            db.DescartarCambios();
            return new(EstadoDecision.NoPermitida, ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (contexto.ArchivoNuevo is not null) archivos.Delete(contexto.ArchivoNuevo);
            return new(EstadoDecision.Conflicto, "El trámite cambió. Recargue la página.");
        }
        catch
        {
            if (contexto.ArchivoNuevo is not null) archivos.Delete(contexto.ArchivoNuevo);
            throw;
        }
    }
}

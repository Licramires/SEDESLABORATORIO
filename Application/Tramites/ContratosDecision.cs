using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed record ActorTramite(int Id, string Rol);
public enum EstadoDecision { Correcta, Inexistente, Conflicto, Invalida, NoPermitida }
public sealed record ResultadoDecision(EstadoDecision Estado, string Mensaje = "");

public interface IDecisionesTramite
{
    Task<ResultadoDecision> Ejecutar(ActorTramite actor, DecisionModel modelo);
}

// Contrato común: validar rol y estado antes de cambiar el expediente; devolver
// el texto auditable. El coordinador de transacción es el único que confirma.
public interface IAccionTramite
{
    IReadOnlyCollection<string> Acciones { get; }
    string Permiso { get; }
    Task<string> Ejecutar(ContextoDecision contexto);
}

public sealed class ContextoDecision(ActorTramite actor, Solicitud solicitud, DecisionModel modelo)
{
    public ActorTramite Actor { get; } = actor;
    public Solicitud Solicitud { get; } = solicitud;
    public DecisionModel Modelo { get; } = modelo;
    public string? ArchivoNuevo { get; set; }
    public static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
    }
    public void ExigirRol(string rol) => Exigir(Actor.Rol == rol, "Su rol no permite esta acción.");
    public void ExigirEstado(params EstadoSolicitud[] estados) => Exigir(estados.Contains(Solicitud.Estado), "La acción no corresponde al estado actual.");
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed class EnviarSolicitud(IRequisitosDatos db, IAlmacenPdf storage, IAvisosRol avisos, IReloj reloj) : IAccionTramite
{
    public string Permiso => "solicitudes";
    public IReadOnlyCollection<string> Acciones { get; } = ["enviar"];
    public async Task<string> Ejecutar(ContextoDecision contexto)
    {
        var s = contexto.Solicitud;
        var model = contexto.Modelo;
        string detail;
        contexto.ExigirRol("propietario"); contexto.ExigirEstado(EstadoSolicitud.Borrador, EstadoSolicitud.Observado);
        ExpedienteService.ValidarDatos(s);
        var requisitos = await ExpedienteService.Requisitos(db, s);
        ExpedienteService.ComprobarDocumentos(s, requisitos, storage);
        if (string.IsNullOrEmpty(s.RequisitosJson)) s.RequisitosJson = JsonSerializer.Serialize(requisitos);
        s.PrimerEnvio ??= reloj.UtcNow;
        s.Estado = EstadoSolicitud.EnRevision; s.FechaEnvio = reloj.UtcNow;
        detail = "Solicitud enviada para revisión documental.";
        await avisos.AvisarRol(RolUsuario.Coordinador, $"Solicitud #{s.Id} recibida para revisión.");
        return detail;
    }
}

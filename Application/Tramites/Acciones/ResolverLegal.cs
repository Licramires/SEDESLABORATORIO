using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed class ResolverLegal(IHabilitacionDatos db, IAlmacenPdf storage, IAvisosRol avisos, IReloj reloj) : IAccionTramite
{
    public string Permiso => "legal";
    public IReadOnlyCollection<string> Acciones { get; } = ["aprobar_legal", "observar_legal", "rechazar_legal"];
    public async Task<string> Ejecutar(ContextoDecision contexto)
    {
        var s = contexto.Solicitud;
        var model = contexto.Modelo;
        var reason = model.Motivo?.Trim() ?? "";
        string detail;
        contexto.ExigirRol("abogado"); contexto.ExigirEstado(EstadoSolicitud.RevisionLegal);
        ContextoDecision.Exigir(reason.Length > 0, "Registre la revisión de licencia y plano.");
        if (model.Accion == "observar_legal")
        {
            ContextoDecision.Exigir(model.DocumentosObservados.Count > 0 && model.DocumentosObservados.Distinct().Count() == model.DocumentosObservados.Count && model.DocumentosObservados.All(id => s.Documentos.Any(d => d.Actual && d.Id == id)), "Seleccione los documentos actuales que deben corregirse.");
            s.Estado = EstadoSolicitud.Observado; s.OrigenObservacion = "legal"; s.RequiereNuevaInspeccion = model.RequiereNuevaInspeccion;
            foreach (var doc in s.Documentos.Where(d => d.Actual && model.DocumentosObservados.Contains(d.Id))) { doc.EstadoRevision = "observado"; doc.Observacion = reason; doc.RevisorId = contexto.Actor.Id; doc.FechaRevision = reloj.UtcNow; }
            detail = $"Observaciones legales: {reason}. {(model.RequiereNuevaInspeccion ? "Requiere nueva inspección." : "Revisar subsanación sin repetir inspección.")}";
        }
        else if (model.Accion == "rechazar_legal")
        { s.Estado = EstadoSolicitud.Rechazado; s.MotivoRechazo = "legal"; s.FechaCierre = reloj.UtcNow; detail = $"Rechazo legal definitivo: {reason}"; }
        else
        {
            var data = ExpedienteService.ValidarDatos(s);
            ExpedienteService.ComprobarDocumentos(s, await ExpedienteService.Requisitos(db, s), storage, revisados: true);
            var ultima = s.Inspecciones.OrderByDescending(i => i.Id).FirstOrDefault();
            ContextoDecision.Exigir(ultima is { Estado: "validada", Resultado: "aprobado" } && storage.Resolve(ultima.ActaPdf) != null, "Se requiere una inspección aprobada y validada con acta disponible.");
            ContextoDecision.Exigir(model.Pdf is not null, "Adjunte la aprobación legal firmada.");
            ContextoDecision.Exigir(!s.EsRenovacion || s.Laboratorio is not null, "La renovación no tiene un establecimiento asociado. Solicite la revisión administrativa del expediente.");
            ContextoDecision.Exigir(s.Laboratorio is null || s.Laboratorio.PropietarioId == s.PropietarioId, "El propietario del expediente no coincide con el del establecimiento. Solicite la revisión administrativa.");
            var start = s.Laboratorio?.VigenciaHasta > reloj.UtcNow ? s.Laboratorio.VigenciaHasta.Value : reloj.UtcNow;
            ContextoDecision.Exigir(start.Year < 9999, "La vigencia registrada no permite agregar un año. Solicite la corrección administrativa antes de habilitar.");
            contexto.ArchivoNuevo = await storage.SaveAsync(model.Pdf!); s.FirmaLegalPdf = contexto.ArchivoNuevo;
            var lab = s.Laboratorio;
            if (lab is null)
            {
                lab = new Laboratorio { Nombre = data.Nombre, Tipo = data.Tipo, Lat = data.Lat, Lng = data.Lng, PropietarioId = s.PropietarioId, Municipio = s.Municipio, Direccion = s.Direccion, Telefono = s.Telefono, Codigo = s.Codigo };
                db.Laboratorios.Add(lab); s.Laboratorio = lab;
            }
            lab.VigenciaHasta = start.AddYears(1); lab.Estado = EstadoLaboratorio.Abierto;
            lab.DatosVerificados = true;
            s.VigenciaOtorgadaDesde = start; s.VigenciaOtorgadaHasta = lab.VigenciaHasta; s.FechaCierre = reloj.UtcNow;
            s.Estado = EstadoSolicitud.Habilitado;
            detail = $"Aprobación legal y cierre. Habilitación vigente hasta {lab.VigenciaHasta:dd/MM/yyyy}. {reason}";
        }
        await avisos.AvisarRol(RolUsuario.Coordinador, $"Solicitud #{s.Id}: {detail}");
        return detail;
    }
}

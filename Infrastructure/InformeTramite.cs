using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

public static class InformeTramite
{
    public static byte[] Crear(Solicitud s, IEnumerable<HistorialTramite> historial)
    {
        var lines = new List<string> { "SEDES - INFORME FINAL DE CIERRE", $"Solicitud #{s.Id} - {TramiteDetalleModel.DatosDe(s).NombreLaboratorio}", $"Municipio: {s.Municipio}",
            $"Fecha de cierre (UTC): {s.FechaCierre:dd/MM/yyyy HH:mm}",
            s.VigenciaOtorgadaHasta.HasValue ? $"Vigencia otorgada: {s.VigenciaOtorgadaDesde:dd/MM/yyyy} a {s.VigenciaOtorgadaHasta:dd/MM/yyyy}" : "Vigencia histórica no registrada en el expediente heredado.", "Historial de decisiones (UTC):" };
        if (!s.FechaCierre.HasValue) lines.Add("Expediente heredado: fecha de cierre no registrada. Informe reconstruido con la información disponible.");
        lines.AddRange(historial.OrderBy(h => h.Fecha).ThenBy(h => h.Id).Select(h => $"{h.Fecha:dd/MM/yyyy HH:mm} - {h.Actor.Nombre}: {h.Detalle}"));
        return SimplePdf.Create(lines);
    }
}

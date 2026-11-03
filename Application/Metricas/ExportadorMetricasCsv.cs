using System.Globalization;
using System.Text;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Metricas;

public interface IExportadorMetricas { byte[] Exportar(MetricasModel modelo); }

public sealed class ExportadorMetricasCsv : IExportadorMetricas
{
    public byte[] Exportar(MetricasModel modelo)
    {
        var rows = modelo.Filas; var borradores = modelo.Borradores; var supervisores = modelo.Supervisores;
        var actividad = modelo.Actividad; var start = modelo.Desde; var end = modelo.Hasta;
        static string Csv(string value)
        {
            if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        static string Numero(double? n) => n?.ToString("0.00", CultureInfo.InvariantCulture) ?? "";
        var csv = new StringBuilder("Municipio,Solicitudes,Habilitadas,Rechazadas,Pendientes,Aperturas,Renovaciones,PromedioDias,TasaAprobacionPorcentaje,Desde,Hasta\r\n");
        foreach (var r in rows) csv.AppendLine($"{Csv(r.Municipio)},{r.Solicitudes},{r.Habilitadas},{r.Rechazadas},{r.Pendientes},{r.Aperturas},{r.Renovaciones},{Numero(r.PromedioDias)},{Numero(r.TasaAprobacion)},{start:yyyy-MM-dd},{end:yyyy-MM-dd}");
        csv.AppendLine($"\r\nBorradoresCreadosEnPeriodo,{borradores}");
        csv.AppendLine("\r\nSupervisor,VisitasAsignadas,ActasRegistradas,Pendientes");
        foreach (var r in supervisores) csv.AppendLine($"{Csv(r.Nombre)},{r.Asignadas},{r.Completadas},{r.Pendientes}");
        csv.AppendLine("\r\nFechaUTC,Funcionario,Solicitud,Accion");
        foreach (var h in actividad) csv.AppendLine($"{h.Fecha:yyyy-MM-dd HH:mm},{Csv(h.Actor.Nombre)},{h.SolicitudId},{Csv(h.Detalle)}");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }
}

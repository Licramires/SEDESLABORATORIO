namespace SEDESLABORATORIO.Models;

// Solo datos autorizados de presentación; nunca serializar entidades de usuarios.
public sealed class AgendaSupervisorModel
{
    public string Hoy { get; set; } = "";
    public string Ahora { get; set; } = "";
    public string Fecha { get; set; } = "";
    public bool SoloRutas { get; set; }
    public List<VisitaSupervisor> Visitas { get; set; } = [];
}

public sealed class VisitaSupervisor
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public string Nombre { get; set; } = "";
    public string Municipio { get; set; } = "";
    public string Direccion { get; set; } = "";
    public bool Renovacion { get; set; }
    public string? Inicio { get; set; }
    public string? Fin { get; set; }
    public bool ActaPreparada { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
}

public sealed record MetricaMes(string Mes, int Aperturas, int Renovaciones);
public sealed record DistribucionMetrica(string Nombre, int Cantidad);
public sealed record AlertaMetrica(string Nombre, int Cantidad, string Explicacion);

public sealed class MetricasVisuales
{
    public string Desde { get; set; } = "";
    public string Hasta { get; set; } = "";
    public string? Municipio { get; set; }
    public DateTime ActualizadoUtc { get; set; }
    public int Total { get; set; }
    public int Habilitadas { get; set; }
    public int Rechazadas { get; set; }
    public int Pendientes { get; set; }
    public int Borradores { get; set; }
    public int CierresConFecha { get; set; }
    public int FechasReconstruidas { get; set; }
    public double? PromedioDias { get; set; }
    public double? TasaAprobacion { get; set; }
    public List<MetricaMes> Meses { get; set; } = [];
    public List<DistribucionMetrica> Tipos { get; set; } = [];
    public List<DistribucionMetrica> Municipios { get; set; } = [];
    public List<RendimientoSupervisor> Supervisores { get; set; } = [];
    public List<AlertaMetrica> Alertas { get; set; } = [];
}

namespace SEDESLABORATORIO.Data.Entities;

public class Solicitud
{
    public int Id { get; set; }
    public int PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;
    public string DatosLaboratorioPropuesto { get; set; } = string.Empty;
    public EstadoSolicitud Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public int Version { get; set; }
    public int? LaboratorioId { get; set; }
    public Laboratorio? Laboratorio { get; set; }
    public bool EsRenovacion { get; set; }
    public string Municipio { get; set; } = "Cochabamba";
    public string FirmaLegalPdf { get; set; } = string.Empty;
    public string OrigenObservacion { get; set; } = string.Empty;
    public string MotivoRechazo { get; set; } = string.Empty;
    public bool RequiereNuevaInspeccion { get; set; }
    public string RequisitosJson { get; set; } = string.Empty;
    public bool RequisitosReconstruidos { get; set; }
    public DateTime? PrimerEnvio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public DateTime? VigenciaOtorgadaDesde { get; set; }
    public DateTime? VigenciaOtorgadaHasta { get; set; }
    public byte[]? InformeCierrePdf { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public ICollection<Inspeccion> Inspecciones { get; set; } = [];
    public ICollection<HistorialTramite> Historial { get; set; } = [];
    public ICollection<Documento> Documentos { get; set; } = [];
    public ICollection<Citacion> Citaciones { get; set; } = [];
}

public enum EstadoSolicitud
{
    EnRevision,
    Aprobado,
    Rechazado,
    Borrador,
    Inspeccion,
    Observado,
    RevisionActa,
    RevisionLegal,
    Habilitado
}

namespace SEDESLABORATORIO.Data.Entities;

public class Documento
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public Solicitud Solicitud { get; set; } = null!;
    public int RequisitoId { get; set; }
    public string ArchivoPdf { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string NombreRequisito { get; set; } = string.Empty;
    public int VersionArchivo { get; set; } = 1;
    public bool Actual { get; set; } = true;
    public string EstadoRevision { get; set; } = "pendiente";
    public string Observacion { get; set; } = string.Empty;
    public int? RevisorId { get; set; }
    public DateTime? FechaRevision { get; set; }
    public DateTime? FechaCarga { get; set; } = DateTime.UtcNow;
}

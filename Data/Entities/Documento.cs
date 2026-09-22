namespace SEDESLABORATORIO.Data.Entities;

public class Documento
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public Solicitud Solicitud { get; set; } = null!;
    public int RequisitoId { get; set; }
    public string ArchivoPdf { get; set; } = string.Empty;
}

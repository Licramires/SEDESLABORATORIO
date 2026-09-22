namespace SEDESLABORATORIO.Data.Entities;

public class Solicitud
{
    public int Id { get; set; }
    public int PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;
    public string DatosLaboratorioPropuesto { get; set; } = string.Empty;
    public EstadoSolicitud Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public ICollection<Documento> Documentos { get; set; } = [];
}

public enum EstadoSolicitud
{
    EnRevision,
    Aprobado,
    Rechazado
}

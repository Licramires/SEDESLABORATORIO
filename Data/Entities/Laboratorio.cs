namespace SEDESLABORATORIO.Data.Entities;

public class Laboratorio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public EstadoLaboratorio Estado { get; set; }
    public string Servicios { get; set; } = string.Empty;
    public string Municipio { get; set; } = "Cochabamba";
    public int? PropietarioId { get; set; }
    public Usuario? Propietario { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool DatosVerificados { get; set; }
}

public enum EstadoLaboratorio
{
    Abierto,
    Cerrado
}

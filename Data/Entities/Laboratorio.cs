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
}

public enum EstadoLaboratorio
{
    Abierto,
    Cerrado
}

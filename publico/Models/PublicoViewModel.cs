namespace SEDESLABORATORIO.Publico.Models;

using SEDESLABORATORIO.Data.Entities;

public class PublicoViewModel
{
    public string? Search { get; set; }
    public string? Tipo { get; set; }
    public EstadoLaboratorio? Estado { get; set; }
    public IReadOnlyList<string> TiposDisponibles { get; set; } = [];
    public IReadOnlyList<LaboratorioPublicoViewModel> Laboratorios { get; set; } = [];
}

public class LaboratorioPublicoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Servicios { get; set; } = string.Empty;
    public bool EstaAbierto => Estado == "abierto";
}

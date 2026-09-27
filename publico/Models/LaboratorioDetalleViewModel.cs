namespace SEDESLABORATORIO.Publico.Models;

public sealed class LaboratorioDetalleViewModel : LaboratorioPublicoViewModel
{
    public string[] ServiciosLista => Servicios
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

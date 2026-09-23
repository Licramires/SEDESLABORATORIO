namespace SEDESLABORATORIO.SolicitudSeguimiento.Models;

public class SolicitudSeguimientoViewModel
{
}

public class SolicitudSeguimientoItemViewModel
{
	public int Id { get; set; }
	public string DatosLaboratorioPropuesto { get; set; } = string.Empty;
	public string Estado { get; set; } = string.Empty;
	public DateTime FechaCreacion { get; set; }
}

public class SolicitudSeguimientoDetalleViewModel : SolicitudSeguimientoItemViewModel
{
	public bool PuedeEnviarse { get; set; }
}

using System.Globalization;
using System.Text.Json;

namespace SEDESLABORATORIO.SolicitudSeguimiento.Models;

public class SolicitudSeguimientoViewModel
{
}

public class SolicitudSeguimientoItemViewModel
{
	public int Id { get; set; }
	public string DatosLaboratorioPropuesto { get; set; } = string.Empty;
	public string NombreLaboratorio { get; set; } = string.Empty;
	public string TipoLaboratorio { get; set; } = string.Empty;
	public decimal Latitud { get; set; }
	public decimal Longitud { get; set; }
	public string Estado { get; set; } = string.Empty;
	public DateTime FechaCreacion { get; set; }

	public string Coordenadas => $"{Latitud.ToString("0.####", CultureInfo.InvariantCulture)}, {Longitud.ToString("0.####", CultureInfo.InvariantCulture)}";

	public void ParseDatosLaboratorio()
	{
		if (string.IsNullOrWhiteSpace(DatosLaboratorioPropuesto))
		{
			return;
		}

		try
		{
			using var document = JsonDocument.Parse(DatosLaboratorioPropuesto);
			var root = document.RootElement;
			NombreLaboratorio = root.TryGetProperty("nombre", out var nombre)
				? nombre.GetString() ?? string.Empty
				: string.Empty;
			TipoLaboratorio = root.TryGetProperty("tipo", out var tipo)
				? tipo.GetString() ?? string.Empty
				: string.Empty;

			if (!root.TryGetProperty("coordenadas", out var coordenadas))
			{
				return;
			}

			if (coordenadas.TryGetProperty("lat", out var latitud))
			{
				Latitud = latitud.GetDecimal();
			}

			if (coordenadas.TryGetProperty("lng", out var longitud))
			{
				Longitud = longitud.GetDecimal();
			}
		}
		catch (JsonException)
		{
			NombreLaboratorio = DatosLaboratorioPropuesto;
		}
	}
}

public class SolicitudSeguimientoDetalleViewModel : SolicitudSeguimientoItemViewModel
{
	public bool PuedeEnviarse { get; set; }
}

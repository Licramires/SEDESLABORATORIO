using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SEDESLABORATORIO.SolicitudRegistro.Models;

public class NuevaSolicitudViewModel
{
    [Required(ErrorMessage = "El nombre del laboratorio es obligatorio.")]
    public string NombreLaboratorio { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de laboratorio es obligatorio.")]
    public string TipoLaboratorio { get; set; } = string.Empty;

    [Required(ErrorMessage = "La latitud es obligatoria.")]
    public decimal Latitud { get; set; }

    [Required(ErrorMessage = "La longitud es obligatoria.")]
    public decimal Longitud { get; set; }

    [Display(Name = "Documentos PDF")]
    public List<IFormFile> DocumentosPdf { get; set; } = [];
}
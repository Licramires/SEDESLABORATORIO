using SEDESLABORATORIO.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SEDESLABORATORIO.SolicitudRegistro.Models;

public class NuevaSolicitudViewModel
{
    [Required(ErrorMessage = "El nombre del laboratorio es obligatorio.")]
    [StringLength(200)]
    [TextoSeguro] public string NombreLaboratorio { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de laboratorio es obligatorio.")]
    [StringLength(100)]
    [TextoSeguro] public string TipoLaboratorio { get; set; } = string.Empty;

    [Required(ErrorMessage = "La latitud es obligatoria.")]
    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitud { get; set; }

    [Required(ErrorMessage = "La longitud es obligatoria.")]
    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitud { get; set; }

    [BindRequired, Required, StringLength(100)]
    [TextoSeguro] public string Municipio { get; set; } = "Cochabamba";
    [StringLength(500)] [TextoSeguro] public string? Direccion { get; set; }
    [Phone(ErrorMessage = "Ingrese un teléfono válido; puede incluir prefijo internacional, espacios o guiones."), StringLength(50)] [TextoSeguro] public string? Telefono { get; set; }
    [StringLength(100)] [TextoSeguro] public string? Codigo { get; set; }

    [Display(Name = "Documentos PDF")]
    public List<IFormFile> DocumentosPdf { get; set; } = [];
}

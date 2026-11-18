using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.SolicitudSeguimiento.Models;
using SEDESLABORATORIO.Infrastructure;

namespace SEDESLABORATORIO.Models;

public sealed class TramiteDetalleModel
{
    public Solicitud Solicitud { get; set; } = null!;
    public List<RequisitoCopia> Requisitos { get; set; } = [];
    public List<Usuario> Supervisores { get; set; } = [];
    public SolicitudSeguimientoItemViewModel Datos => DatosDe(Solicitud);
    public static SolicitudSeguimientoItemViewModel DatosDe(Solicitud solicitud)
    {
        var result = new SolicitudSeguimientoItemViewModel { DatosLaboratorioPropuesto = solicitud.DatosLaboratorioPropuesto };
        result.ParseDatosLaboratorio();
        return result;
    }
}

public sealed class DecisionModel
{
    [BindRequired] public int Id { get; set; }
    [BindRequired] public int Version { get; set; }
    [Required, StringLength(40)] public string Accion { get; set; } = "";
    [StringLength(4000)] [TextoSeguro(Multilinea = true)] public string? Motivo { get; set; }
    public int? SupervisorId { get; set; }
    [FechaCivil] public DateTime? Fecha { get; set; }
    [FechaCivil] public DateTime? FechaFin { get; set; }
    public List<int> DocumentosObservados { get; set; } = [];
    public bool RequiereNuevaInspeccion { get; set; }
    [StringLength(30)] public string? Resultado { get; set; }
    [StringLength(2000)] [TextoSeguro(Multilinea = true)] public string? Citacion { get; set; }
    [Range(1, 90)] public int DiasSubsanacion { get; set; } = 15;
    public IFormFile? Pdf { get; set; }
    public List<VerificacionForm> Verificaciones { get; set; } = [];
}

public sealed class UsuarioForm
{
    public int Version { get; set; }
    public int Id { get; set; }
    [Required, StringLength(120)] [TextoSeguro] public string Nombre { get; set; } = "";
    [Required, EmailAddress, CorreoValido, StringLength(200)] [TextoSeguro] public string Email { get; set; } = "";
    [StringLength(128, MinimumLength = 10)] public string? Password { get; set; }
    [BindRequired, EnumDataType(typeof(RolUsuario))] public RolUsuario Rol { get; set; }
    [BindRequired] public bool Activo { get; set; } = true;
}

public sealed class RequisitoForm
{
    public int Id { get; set; }
    [Required, StringLength(200)] [TextoSeguro] public string Nombre { get; set; } = "";
    [BindRequired] public bool Obligatorio { get; set; }
    [BindRequired] public bool Activo { get; set; } = true;
    public int Version { get; set; }
    public int? SeccionId { get; set; }
    [Range(0, 9999)] public int Orden { get; set; }
    public IFormFile? Plantilla { get; set; }
}

public sealed class SeccionForm
{
    public int Id { get; set; }
    public int Version { get; set; }
    [Required, StringLength(200)] [TextoSeguro] public string Nombre { get; set; } = "";
    [StringLength(1000)] [TextoSeguro(Multilinea = true)] public string? Descripcion { get; set; }
    [Range(0, 9999)] public int Orden { get; set; }
    [BindRequired] public bool Activa { get; set; } = true;
}

public sealed class RequisitosAdminModel
{
    public List<Requisito> Requisitos { get; set; } = [];
    public List<SeccionRequisito> Secciones { get; set; } = [];
}

public sealed class RolesAdminModel
{
    public List<PermisoRol> Permisos { get; set; } = [];
    public Dictionary<RolUsuario, int> Usuarios { get; set; } = [];
    public List<CambioPermiso> Cambios { get; set; } = [];
}

public sealed class PermisoFila
{
    [Required] [TextoSeguro] public string Clave { get; set; } = "";
    [BindRequired] public bool Habilitado { get; set; }
    [BindRequired, Range(0, int.MaxValue)] public int Version { get; set; }
}

public sealed class PermisosForm
{
    [BindRequired, EnumDataType(typeof(RolUsuario))] public RolUsuario Rol { get; set; }
    public List<PermisoFila> Permisos { get; set; } = [];
}

public sealed class VerificacionForm
{
    public int Id { get; set; }
    [Required, RegularExpression("^(cumple|observado|no_aplica)$")] public string Resultado { get; set; } = "";
    [StringLength(2000)] [TextoSeguro(Multilinea = true)] public string? Observacion { get; set; }
}

public sealed class RevisionDocumentoForm
{
    [BindRequired] public int Id { get; set; }
    [BindRequired] public int DocumentoId { get; set; }
    [BindRequired] public int Version { get; set; }
    [Required, RegularExpression("^(aprobado|observado)$")] public string Resultado { get; set; } = "";
    [Required, StringLength(2000)] [TextoSeguro(Multilinea = true)] public string Motivo { get; set; } = "";
}

public sealed class DocumentoPantalla
{
    public Documento Documento { get; set; } = null!;
    public Solicitud Solicitud { get; set; } = null!;
    public bool Revisable { get; set; }
}

public sealed class CitacionForm
{
    [BindRequired] public int Id { get; set; }
    [BindRequired] public int InspeccionId { get; set; }
    [BindRequired] public int Version { get; set; }
    [Required, StringLength(2000)] [TextoSeguro(Multilinea = true)] public string Motivo { get; set; } = "";
    [BindRequired, FechaCivil(SoloFecha = true)] public DateTime FechaEmision { get; set; }
    [BindRequired, FechaCivil(SoloFecha = true)] public DateTime Vencimiento { get; set; }
    [Range(0, 30)] public int AnticipacionDias { get; set; } = 5;
    [Required] public IFormFile? Evidencia { get; set; }
}

public sealed class CatalogoForm
{
    public int Id { get; set; }
    public int Version { get; set; }
    [BindRequired, Required, RegularExpression("^(tipo|municipio|inspeccion|feriado|formulario_acta|formulario_citacion)$")] [TextoSeguro] public string Clase { get; set; } = "tipo";
    [Required, StringLength(200)] [TextoSeguro] public string Nombre { get; set; } = "";
    [StringLength(1000)] [TextoSeguro(Multilinea = true)] public string? Referencia { get; set; }
    [BindRequired] public bool Activo { get; set; } = true;
    public IFormFile? Plantilla { get; set; }
}

public sealed class MetricaMunicipio
{
    public string Municipio { get; set; } = "";
    public int Solicitudes { get; set; }
    public int Habilitadas { get; set; }
    public int Rechazadas { get; set; }
    public int Pendientes { get; set; }
    public int Aperturas { get; set; }
    public int Renovaciones { get; set; }
    public double? PromedioDias { get; set; }
    public double? TasaAprobacion => Habilitadas + Rechazadas == 0 ? null : 100d * Habilitadas / (Habilitadas + Rechazadas);
}
public sealed class MetricasModel
{
    public MetricasVisuales Visuales { get; set; } = new();
    public List<string> MunicipiosDisponibles { get; set; } = [];
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public string? Municipio { get; set; }
    public List<MetricaMunicipio> Filas { get; set; } = [];
    public List<RendimientoSupervisor> Supervisores { get; set; } = [];
    public List<HistorialTramite> Actividad { get; set; } = [];
    public int Borradores { get; set; }
}

public sealed class RendimientoSupervisor
{
    [TextoSeguro] public string Nombre { get; set; } = "";
    public int Asignadas { get; set; }
    public int Completadas { get; set; }
    public int Pendientes { get; set; }
}

public sealed class DatosBorradorForm : SEDESLABORATORIO.SolicitudRegistro.Models.NuevaSolicitudViewModel
{
    [BindRequired] public int Id { get; set; }
    [BindRequired] public int Version { get; set; }
}

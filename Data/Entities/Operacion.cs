namespace SEDESLABORATORIO.Data.Entities;

public class PermisoRol
{
    public RolUsuario Rol { get; set; }
    public string Clave { get; set; } = "";
    public bool Habilitado { get; set; } = true;
    public int Version { get; set; } = 1;
}

public class CambioPermiso
{
    public int Id { get; set; }
    public RolUsuario Rol { get; set; }
    public string Clave { get; set; } = "";
    public bool Habilitado { get; set; }
    public string Actor { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public class SeccionRequisito
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
    public bool Eliminada { get; set; }
    public int Version { get; set; } = 1;
}

public class Requisito
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Obligatorio { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public int? SeccionId { get; set; }
    public SeccionRequisito? Seccion { get; set; }
    public int Orden { get; set; }
    public int Version { get; set; } = 1;
    public string PlantillaPdf { get; set; } = string.Empty;
}

public class Inspeccion
{
    public string NotasAdicionales { get; set; } = "";
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public Solicitud Solicitud { get; set; } = null!;
    public int SupervisorId { get; set; }
    public Usuario Supervisor { get; set; } = null!;
    // Hora civil de Bolivia (UTC-4), sin conversión según zona del servidor.
    public DateTime Fecha { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Programada { get; set; } = true;
    public string ListaVerificacionJson { get; set; } = string.Empty;
    public string PlantillaPdf { get; set; } = string.Empty;
    public int PlantillaVersion { get; set; }
    public int DiasSubsanacion { get; set; } = 15;
    public string Estado { get; set; } = "pendiente";
    public string Resultado { get; set; } = string.Empty;
    public string Acta { get; set; } = string.Empty;
    public string ActaPdf { get; set; } = string.Empty;
    public string Citacion { get; set; } = string.Empty;
    public DateTime? PlazoSubsanacion { get; set; }
}

public class Citacion
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public Solicitud Solicitud { get; set; } = null!;
    public int InspeccionId { get; set; }
    public Inspeccion Inspeccion { get; set; } = null!;
    public int SupervisorId { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime Vencimiento { get; set; }
    public int AnticipacionDias { get; set; } = 5;
    public string Motivo { get; set; } = string.Empty;
    public string EvidenciaPdf { get; set; } = string.Empty;
    public DateTime? FechaAtencion { get; set; }
}

// Los catálogos oficiales los configura la institución; no se inventan valores iniciales.
public class CatalogoItem
{
    public int Id { get; set; }
    public string Clase { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public int Version { get; set; } = 1;
    public string ArchivoPdf { get; set; } = string.Empty;
}

public class HistorialTramite
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public Solicitud Solicitud { get; set; } = null!;
    public int ActorId { get; set; }
    public Usuario Actor { get; set; } = null!;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Detalle { get; set; } = string.Empty;
}

public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public bool Leida { get; set; }
    public string? Clave { get; set; }
}

using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Laboratorio> Laboratorios => Set<Laboratorio>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Requisito> Requisitos => Set<Requisito>();
    public DbSet<SeccionRequisito> SeccionesRequisitos => Set<SeccionRequisito>();
    public DbSet<CambioPermiso> CambiosPermisos => Set<CambioPermiso>();
    public DbSet<Inspeccion> Inspecciones => Set<Inspeccion>();
    public DbSet<HistorialTramite> HistorialTramites => Set<HistorialTramite>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PermisoRol> PermisosRol => Set<PermisoRol>();
    public DbSet<Citacion> Citaciones => Set<Citacion>();
    public DbSet<CatalogoItem> Catalogos => Set<CatalogoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().Property(u => u.Version).IsConcurrencyToken();
        modelBuilder.Entity<Inspeccion>().HasIndex(i => new { i.SupervisorId, i.Fecha });
        modelBuilder.Entity<CatalogoItem>().HasIndex(c => new { c.Clase, c.Nombre }).IsUnique();
        modelBuilder.Entity<CatalogoItem>().Property(c => c.Nombre).UseCollation("NOCASE");
        modelBuilder.Entity<CatalogoItem>().Property(c => c.Version).IsConcurrencyToken();
        modelBuilder.Entity<Requisito>().Property(r => r.Version).IsConcurrencyToken();
        modelBuilder.Entity<SeccionRequisito>().Property(r => r.Version).IsConcurrencyToken();
        modelBuilder.Entity<PermisoRol>().Property(r => r.Version).IsConcurrencyToken();
        modelBuilder.Entity<Requisito>().HasOne(r => r.Seccion).WithMany().HasForeignKey(r => r.SeccionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Citacion>().HasOne(c => c.Inspeccion).WithMany().HasForeignKey(c => c.InspeccionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Citacion>().HasOne<Usuario>().WithMany().HasForeignKey(c => c.SupervisorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Documento>().HasOne<Usuario>().WithMany().HasForeignKey(d => d.RevisorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PermisoRol>().HasKey(p => new { p.Rol, p.Clave });
        modelBuilder.Entity<PermisoRol>().Property(p => p.Rol).HasConversion<string>();
        modelBuilder.Entity<Usuario>().HasIndex(usuario => usuario.Email).IsUnique();
        modelBuilder.Entity<Usuario>().Property(usuario => usuario.Rol)
            .HasConversion(rol => rol.ToString().ToLowerInvariant(), valor => Enum.Parse<RolUsuario>(valor, true));
        modelBuilder.Entity<Laboratorio>().Property(laboratorio => laboratorio.Estado)
            .HasConversion(estado => estado.ToString().ToLowerInvariant(), valor => Enum.Parse<EstadoLaboratorio>(valor, true));
        modelBuilder.Entity<Solicitud>().Property(solicitud => solicitud.Estado)
            .HasConversion(estado => EstadoTexto(estado), valor => ParseEstado(valor));
        modelBuilder.Entity<Solicitud>().Property(s => s.Version).IsConcurrencyToken();
        modelBuilder.Entity<Usuario>().Property(u => u.Email).UseCollation("NOCASE");
        modelBuilder.Entity<Notificacion>().HasIndex(n => n.Clave).IsUnique();
        modelBuilder.Entity<Laboratorio>().HasOne(l => l.Propietario).WithMany().HasForeignKey(l => l.PropietarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Solicitud>().HasOne(s => s.Laboratorio).WithMany().HasForeignKey(s => s.LaboratorioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Inspeccion>().HasOne(i => i.Supervisor).WithMany().HasForeignKey(i => i.SupervisorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HistorialTramite>().HasOne(h => h.Actor).WithMany().HasForeignKey(h => h.ActorId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Solicitud>()
            .HasOne(solicitud => solicitud.Propietario)
            .WithMany(usuario => usuario.Solicitudes)
            .HasForeignKey(solicitud => solicitud.PropietarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Documento>()
            .HasOne(documento => documento.Solicitud)
            .WithMany(solicitud => solicitud.Documentos)
            .HasForeignKey(documento => documento.SolicitudId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public static string EstadoEtiqueta(EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.EnRevision => "Revisión documental", EstadoSolicitud.Aprobado => "Por asignar inspección",
        EstadoSolicitud.Borrador => "Borrador", EstadoSolicitud.Inspeccion => "Inspección",
        EstadoSolicitud.Observado => "Por subsanar", EstadoSolicitud.RevisionActa => "Revisión de acta",
        EstadoSolicitud.RevisionLegal => "Revisión legal", EstadoSolicitud.Habilitado => "Habilitado",
        EstadoSolicitud.Rechazado => "Rechazado", _ => estado.ToString()
    };

    public static string EstadoTexto(EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.EnRevision => "en_revision",
        EstadoSolicitud.RevisionActa => "revision_acta",
        EstadoSolicitud.RevisionLegal => "revision_legal",
        _ => estado.ToString().ToLowerInvariant()
    };

    public static EstadoSolicitud ParseEstado(string valor) => Enum.Parse<EstadoSolicitud>(valor.Replace("_", ""), true);
}

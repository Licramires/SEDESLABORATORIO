using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Laboratorio> Laboratorios => Set<Laboratorio>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<Documento> Documentos => Set<Documento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().Property(usuario => usuario.Rol)
            .HasConversion(rol => rol.ToString().ToLowerInvariant(), valor => Enum.Parse<RolUsuario>(valor, true));
        modelBuilder.Entity<Laboratorio>().Property(laboratorio => laboratorio.Estado)
            .HasConversion(estado => estado.ToString().ToLowerInvariant(), valor => Enum.Parse<EstadoLaboratorio>(valor, true));
        modelBuilder.Entity<Solicitud>().Property(solicitud => solicitud.Estado)
            .HasConversion(estado => estado.ToString().ToLowerInvariant(), valor => Enum.Parse<EstadoSolicitud>(valor, true));

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
}

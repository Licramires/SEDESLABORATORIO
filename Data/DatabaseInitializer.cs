using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        if (!environment.IsDevelopment()
            || !configuration.GetValue<bool>("Auth:SeedDemoUsers"))
        {
            return;
        }

        if (!await context.Usuarios.AnyAsync())
        {
            var passwordHasher = new PasswordHasher<Usuario>();
            var demoUsers = new[]
            {
                CreateUser("Propietario Demo", "propietario@si-lab.local", RolUsuario.Propietario),
                CreateUser("Coordinador Demo", "coordinador@si-lab.local", RolUsuario.Coordinador),
                CreateUser("Supervisor Demo", "supervisor@si-lab.local", RolUsuario.Supervisor),
                CreateUser("Gerente Demo", "gerente@si-lab.local", RolUsuario.Gerente),
                CreateUser("Administrador Demo", "administrador@si-lab.local", RolUsuario.Administrador)
            };

            foreach (var user in demoUsers)
            {
                user.Password = passwordHasher.HashPassword(user, "Demo123!");
            }

            await context.Usuarios.AddRangeAsync(demoUsers);
        }

        if (!await context.Laboratorios.AnyAsync())
        {
            await context.Laboratorios.AddRangeAsync(
                new Laboratorio
                {
                    Nombre = "Laboratorio Central SEDES",
                    Tipo = "Clínico",
                    Lat = -17.3935m,
                    Lng = -66.1570m,
                    Estado = EstadoLaboratorio.Abierto,
                    Servicios = "Hematología, Química clínica, Microbiología"
                },
                new Laboratorio
                {
                    Nombre = "Laboratorio Nova",
                    Tipo = "Bacteriológico",
                    Lat = -17.3892m,
                    Lng = -66.1634m,
                    Estado = EstadoLaboratorio.Cerrado,
                    Servicios = "Cultivos, Análisis bacteriológico"
                },
                new Laboratorio
                {
                    Nombre = "Laboratorio Clínica Azul",
                    Tipo = "Especializado",
                    Lat = -17.4011m,
                    Lng = -66.1512m,
                    Estado = EstadoLaboratorio.Abierto,
                    Servicios = "Genética, Inmunología, Biología molecular"
                });
        }

        await context.SaveChangesAsync();
    }

    private static Usuario CreateUser(string name, string email, RolUsuario role) => new()
    {
        Nombre = name,
        Email = email,
        Rol = role
    };
}

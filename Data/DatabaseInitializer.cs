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
            || !configuration.GetValue<bool>("Auth:SeedDemoUsers")
            || await context.Usuarios.AnyAsync())
        {
            return;
        }

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
        await context.SaveChangesAsync();
    }

    private static Usuario CreateUser(string name, string email, RolUsuario role) => new()
    {
        Nombre = name,
        Email = email,
        Rol = role
    };
}

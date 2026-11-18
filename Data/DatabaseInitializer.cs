using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;

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
        await DemoDataSeeder.CorregirRotulosAsync(context);

        var cierres = await context.Solicitudes.Include(s => s.Historial).ThenInclude(h => h.Actor)
            .Where(s => s.Estado == EstadoSolicitud.Habilitado && s.InformeCierrePdf == null).ToListAsync();
        foreach (var s in cierres) s.InformeCierrePdf = InformeTramite.Crear(s, s.Historial);
        if (cierres.Count > 0) await context.SaveChangesAsync();

        // Primer acceso de producción: secretos por variables de entorno, nunca en el repositorio.
        if (!environment.IsDevelopment() && !await context.Usuarios.AnyAsync())
        {
            var email = configuration["Bootstrap:AdminEmail"];
            var password = configuration["Bootstrap:AdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || !CorreoValidoAttribute.EsDireccion(email) || string.IsNullOrEmpty(password) || password.Length < 12)
                throw new InvalidOperationException("Configure Bootstrap:AdminEmail y Bootstrap:AdminPassword (mínimo 12 caracteres) para crear la primera cuenta administrativa.");
            var admin = CreateUser("Administrador", email.Trim().ToLowerInvariant(), RolUsuario.Administrador);
            admin.Password = new PasswordHasher<Usuario>().HashPassword(admin, password);
            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();
        }

        if (environment.IsDevelopment() && !await context.Requisitos.AnyAsync())
        {
            context.Requisitos.AddRange(new Requisito { Nombre = "Licencia municipal", Obligatorio = true },
                new Requisito { Nombre = "Plano arquitectónico", Obligatorio = true },
                new Requisito { Nombre = "Certificado sanitario", Obligatorio = true });
            await context.SaveChangesAsync();
        }

        if (!environment.IsDevelopment()
            || !configuration.GetValue<bool>("Auth:SeedDemoUsers"))
        {
            return;
        }

        {
            var passwordHasher = new PasswordHasher<Usuario>();
            var demoUsers = new[]
            {
                CreateUser("Propietario Demo", "propietario@si-lab.local", RolUsuario.Propietario),
                CreateUser("Coordinador Demo", "coordinador@si-lab.local", RolUsuario.Coordinador),
                CreateUser("Supervisor Demo", "supervisor@si-lab.local", RolUsuario.Supervisor),
                CreateUser("Gerente Demo", "gerente@si-lab.local", RolUsuario.Gerente),
                CreateUser("Administrador Demo", "administrador@si-lab.local", RolUsuario.Administrador),
                CreateUser("Abogado Demo", "abogado@si-lab.local", RolUsuario.Abogado)
            };

            foreach (var user in demoUsers)
            {
                if (await context.Usuarios.AnyAsync(u => u.Email == user.Email)) continue;
                user.Password = passwordHasher.HashPassword(user, "Demo123!");
                context.Usuarios.Add(user);
            }
        }

        await context.SaveChangesAsync();
        if (configuration.GetValue("Auth:SeedDemoData", true))
            await DemoDataSeeder.SeedAsync(context, scope.ServiceProvider.GetRequiredService<PdfStorage>());
    }

    private static Usuario CreateUser(string name, string email, RolUsuario role) => new()
    {
        Nombre = name,
        Email = email,
        Rol = role
    };
}

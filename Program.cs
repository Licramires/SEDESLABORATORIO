using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Auth.Services;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Infrastructure.Routing;

namespace SEDESLABORATORIO
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddRazorOptions(options =>
                {
                    options.ViewLocationFormats.Add("/{2}/Views/{1}/{0}.cshtml");
                    options.ViewLocationFormats.Add("/{2}/Views/Shared/{0}.cshtml");
                    options.AreaViewLocationFormats.Add("/{2}/Views/{1}/{0}.cshtml");
                    options.AreaViewLocationFormats.Add("/{2}/Views/Shared/{0}.cshtml");
                });
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddAuthentication("SiLabCookie")
                .AddCookie("SiLabCookie", options =>
                {
                    options.LoginPath = "/auth/Auth/Login";
                    options.AccessDeniedPath = "/auth/Auth/AccessDenied";
                    options.Cookie.Name = "si-lab-auth";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });
            builder.Services.AddAuthorization();
            builder.Services.AddScoped<IAuthService, AuthService>();

            var app = builder.Build();
            await DatabaseInitializer.InitializeAsync(app.Services, app.Environment, app.Configuration);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapApplicationRoutes();

            app.Run();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Infrastructure.Routing;

namespace SEDESLABORATORIO
{
    public class Program
    {
        public static void Main(string[] args)
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

            var app = builder.Build();

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

            app.UseAuthorization();

            app.MapApplicationRoutes();

            app.Run();
        }
    }
}

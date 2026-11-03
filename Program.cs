using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Auth.Services;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Infrastructure.Routing;
using SEDESLABORATORIO.Infrastructure;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;

namespace SEDESLABORATORIO
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(o => { o.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider()); o.ModelBinderProviders.Insert(0, new BooleanModelBinderProvider()); o.Filters.AddService<PermisoFilter>(); o.Filters.AddService<RecuperacionOperacionFilter>(); })
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
            builder.Services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            });
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<PdfStorage>();
            builder.Services.AddScoped<PermisoFilter>();
            builder.Services.AddScoped<RecuperacionOperacionFilter>();
            builder.Services.AddSiLabApplication();
            builder.Services.AddSingleton<LocalDatabaseLease>();
            builder.Services.AddHostedService<VigenciaWorker>();
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = 55 * 1024 * 1024);
            builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 55 * 1024 * 1024);
            builder.Services.Configure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>("SiLabCookie", o =>
            {
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
                o.Events.OnValidatePrincipal = async ctx =>
                {
                    var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var validId = int.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
                    var user = validId ? await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id && u.Activo) : null;
                    if (user is null || user.Rol.ToString().ToLowerInvariant() != ctx.Principal?.FindFirstValue(ClaimTypes.Role) || user.SelloSeguridad != ctx.Principal?.FindFirstValue("security_stamp"))
                    {
                        ctx.RejectPrincipal();
                        await ctx.HttpContext.SignOutAsync("SiLabCookie");
                    }
                };
            });

            var app = builder.Build();
            _ = app.Services.GetRequiredService<LocalDatabaseLease>();
            await DatabaseInitializer.InitializeAsync(app.Services, app.Environment, app.Configuration);
            await StorageRecovery.RecoverAsync(app.Services);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                if (context.Request.Path.StartsWithSegments("/uploads")) { context.Response.StatusCode = 404; return; }
                await next();
            });
            app.UseStaticFiles();

            app.UseRouting();
            app.Use(async (context, next) =>
            {
                var limit = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata>()?.MaxRequestBodySize ?? 55L * 1024 * 1024;
                if (context.Request.ContentLength > limit)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("La carga supera el tamaño permitido para esta operación.");
                    return;
                }
                try { await next(); }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode is 5 or 6 && !context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("Otra operación está actualizando los datos. Recargue la página e intente nuevamente.");
                }
                catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 or 6 } && !context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("Otra operación está actualizando los datos. Recargue la página e intente nuevamente.");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && ex is not BadHttpRequestException && ex is not InvalidDataException && !context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
                {
                    app.Logger.LogError(ex, "No se pudo completar una operación de almacenamiento.");
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.Headers.RetryAfter = "5";
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("El almacenamiento no está disponible. Consulte el estado del trámite antes de reenviar y solicite la revisión del servicio.");
                }
            });
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapApplicationRoutes();

            app.Run();
        }
    }
}

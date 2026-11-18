using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Infrastructure;

// SQLite y el sistema de archivos no comparten transacción. Un corte entre ambos puede dejar
// un archivo sin registro. Se conserva en recuperación privada; nunca se borra una evidencia.
public static class StorageRecovery
{
    public static async Task RecoverAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<PdfStorage>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StorageRecovery");
        if (!Directory.Exists(storage.Root)) return;
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(IEnumerable<string> keys) { foreach (var key in keys) if (!string.IsNullOrWhiteSpace(key)) referenced.Add(key); }
        Add(await db.Documentos.Select(d => d.ArchivoPdf).ToListAsync());
        Add(await db.Inspecciones.Select(i => i.ActaPdf).ToListAsync());
        Add(await db.Inspecciones.Select(i => i.PlantillaPdf).ToListAsync());
        Add(await db.Citaciones.Select(c => c.EvidenciaPdf).ToListAsync());
        Add(await db.Catalogos.Select(c => c.ArchivoPdf).ToListAsync());
        Add(await db.Requisitos.Select(r => r.PlantillaPdf).ToListAsync());
        Add(await db.Solicitudes.Select(s => s.FirmaLegalPdf).ToListAsync());
        // Las plantillas antiguas también pueden estar referenciadas solo por una copia congelada.
        foreach (var json in await db.Solicitudes.Where(s => s.RequisitosJson != "").Select(s => s.RequisitosJson).ToListAsync())
        {
            try { Add(ExpedienteService.LeerRequisitos(new Solicitud { RequisitosJson = json }).Select(r => r.PlantillaPdf)); }
            catch (InvalidOperationException)
            {
                logger.LogWarning("Se omite la recuperación de archivos: una copia histórica requiere revisión.");
                return;
            }
        }
        var batch = Path.Combine(storage.Root, "recuperacion", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
        var recovered = new List<string>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(storage.Root))
            {
                var name = Path.GetFileName(path);
                if (referenced.Contains(name) || !Regex.IsMatch(name, @"\A[a-fA-F0-9]{32}\.(pdf|jpg|jpeg|png)\z", RegexOptions.IgnoreCase)) continue;
                Directory.CreateDirectory(batch);
                File.Move(path, Path.Combine(batch, name));
                recovered.Add(name);
            }
            if (recovered.Count > 0)
            {
                await File.WriteAllTextAsync(Path.Combine(batch, "manifest.json"), JsonSerializer.Serialize(new { FechaUtc = DateTime.UtcNow, Archivos = recovered, Motivo = "Sin referencia persistida al arrancar; conservados para recuperación administrativa." }));
                logger.LogWarning("Se conservaron {Count} archivos sin registro en la carpeta privada de recuperación.", recovered.Count);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "No se completó la recuperación de archivos; se conservan los archivos para revisión.");
        }
    }
}

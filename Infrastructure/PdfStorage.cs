using System.Text;

namespace SEDESLABORATORIO.Infrastructure;

public sealed class PdfStorage(IWebHostEnvironment environment, IConfiguration configuration) : IAlmacenPdf
{
    public string Root { get; } = Path.GetFullPath(configuration["Storage:Root"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "documentos"));
    public const long MaxSize = 10 * 1024 * 1024;
    public static string DisplayName(IFormFile file)
    {
        var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
        return name.Length > 200 ? name[..200] : name;
    }

    public static async Task<string?> ValidateAsync(IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxSize) return "Cada PDF debe tener contenido y no superar los 10 MB.";
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase)) return "Solo se permiten archivos PDF.";
        await using var stream = file.OpenReadStream();
        var header = new byte[5];
        var read = await stream.ReadAtLeastAsync(header, 5, false);
        if (read != 5 || Encoding.ASCII.GetString(header) != "%PDF-") return "El contenido del archivo no corresponde a un PDF.";
        if (stream.CanSeek)
        {
            stream.Seek(Math.Max(0, stream.Length - 1024), SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.Latin1);
            if (!(await reader.ReadToEndAsync()).Contains("%%EOF", StringComparison.Ordinal)) return "El PDF está incompleto.";
        }
        return null;
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken token = default)
    {
        var error = await ValidateAsync(file);
        if (error is not null) throw new InvalidOperationException(error);
        Directory.CreateDirectory(Root);
        var key = Guid.NewGuid().ToString("N") + ".pdf";
        try
        {
            await using var stream = File.Create(Path.Combine(Root, key));
            await file.CopyToAsync(stream, token);
            return key;
        }
        catch { Delete(key); throw; }
    }

    public string? Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        // Los archivos heredados permanecen inaccesibles por StaticFiles.
        var root = key.StartsWith("uploads/solicitudes/", StringComparison.Ordinal)
            ? Path.Combine(environment.WebRootPath, "uploads", "solicitudes") : Root;
        var relative = key.StartsWith("uploads/solicitudes/", StringComparison.Ordinal) ? key[20..] : key;
        string path;
        try { path = Path.GetFullPath(Path.Combine(root, relative)); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return null; }
        return path.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(path) ? path : null;
    }

    public void Delete(string key)
    {
        var path = Resolve(key);
        if (path is not null) File.Delete(path);
    }
}

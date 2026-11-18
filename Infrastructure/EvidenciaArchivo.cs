namespace SEDESLABORATORIO.Infrastructure;

public static class EvidenciaArchivo
{
    public static async Task<string?> Validar(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext == ".pdf") return await PdfStorage.ValidateAsync(file);
        if (file.Length < 16 || file.Length > PdfStorage.MaxSize || ext is not (".jpg" or ".jpeg" or ".png"))
            return "Adjunte una evidencia PDF, JPG o PNG de hasta 10 MB.";
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream(); await input.CopyToAsync(buffer);
        var data = buffer.ToArray();
        var valido = ext == ".png"
            ? data.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10}) && data.AsSpan(data.Length-8,4).SequenceEqual("IEND"u8)
            : data[0] == 255 && data[1] == 216 && data[2] == 255 && data[^2] == 255 && data[^1] == 217;
        return valido ? null : "La imagen está incompleta o su contenido no coincide con el formato indicado.";
    }

    public static async Task<string> Guardar(IFormFile file, IAlmacenPdf storage)
    {
        var error = await Validar(file); if (error is not null) throw new InvalidOperationException(error);
        if (Path.GetExtension(file.FileName).Equals(".pdf",StringComparison.OrdinalIgnoreCase)) return await storage.SaveAsync(file);
        Directory.CreateDirectory(storage.Root);
        var key = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        try { await using var output = File.Create(Path.Combine(storage.Root,key)); await file.CopyToAsync(output); return key; }
        catch { storage.Delete(key); throw; }
    }

    public static string Tipo(string key) => Path.GetExtension(key).ToLowerInvariant() switch
    { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", _ => "application/pdf" };
}

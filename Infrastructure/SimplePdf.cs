using System.Text;

namespace SEDESLABORATORIO.Infrastructure;

// Documento textual multipágina; WinAnsi conserva los acentos del formulario.
public static class SimplePdf
{
    public static byte[] Create(IEnumerable<string> lines)
    {
        var wrapped = lines.SelectMany(line => Wrap(line)).ToList();
        var pages = wrapped.Chunk(45).ToList();
        if (pages.Count == 0) pages.Add([""]);
        var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", "", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" };
        var kids = new List<string>();
        foreach (var page in pages)
        {
            var pageId = objects.Count + 1;
            kids.Add($"{pageId} 0 R");
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {pageId + 1} 0 R >>");
            var content = "BT /F1 11 Tf 50 790 Td 16 TL\n" + string.Join("\n", page.Select(s => $"({Escape(s)}) Tj T*")) + "\nET";
            objects.Add($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }
        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", kids)}] /Count {pages.Count} >>";
        using var output = new MemoryStream();
        void Write(string value) => output.Write(Encoding.Latin1.GetBytes(value));
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++) { offsets.Add(output.Position); Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = output.Position;
        Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:D10} 00000 n \n");
        Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }
    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", " ");
    private static IEnumerable<string> Wrap(string s)
    {
        foreach (var line in s.Split('\n'))
        {
            if (line.Length == 0) yield return "";
            for (var i = 0; i < line.Length; i += 85) yield return line.Substring(i, Math.Min(85, line.Length - i));
        }
    }
}

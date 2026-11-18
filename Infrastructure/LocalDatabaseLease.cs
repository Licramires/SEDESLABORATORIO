using Microsoft.Data.Sqlite;

namespace SEDESLABORATORIO.Infrastructure;

// El contrato usa una instancia por SQLite. Mantener la exclusión durante toda la vida
// del proceso impide que un segundo arranque recupere archivos de una escritura activa.
public sealed class LocalDatabaseLease : IDisposable
{
    private readonly FileStream? lease;
    public LocalDatabaseLease(IConfiguration configuration)
    {
        var source = new SqliteConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection")).DataSource;
        if (source is "" or ":memory:") return;
        var path = Path.GetFullPath(source) + ".instance.lock";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try { lease = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException ex) { throw new InvalidOperationException("No se pudo adquirir el acceso exclusivo de esta instancia SQLite. Compruebe que no haya otro proceso de SI_Lab usando la misma base antes de iniciar.", ex); }
    }
    public void Dispose() => lease?.Dispose();
}

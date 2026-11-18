namespace SEDESLABORATORIO.Application.Abstractions;

public interface ILectorArchivos
{
    string? Resolve(string key);
}

public interface IAlmacenPdf : ILectorArchivos
{
    string Root { get; }
    Task<string> SaveAsync(IFormFile file, CancellationToken token = default);
    void Delete(string key);
}

public interface IReloj
{
    DateTime UtcNow { get; }
    DateTime HoraBolivia { get; }
}

public sealed class RelojSistema : IReloj
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime HoraBolivia => UtcNow.AddHours(-4);
}

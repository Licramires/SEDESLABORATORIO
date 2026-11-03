using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SEDESLABORATORIO.Data;

// Un solo contexto por petición y una sola unidad de trabajo, incluso cuando
// distintos consumidores reciben puertos diferentes de la misma instancia.
public partial class ApplicationDbContext : IGestionDatos, IAdministracionDatos, IAutenticacionDatos,
    IPublicoDatos, IPermisosDatos, IRegistroHistorialDatos, IAvisosDatos, IAlertasDatos,
    IDecisionesDatos, IAsignacionesDatos, IHabilitacionDatos
{
    public async Task<ITransaccionDatos> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new TransaccionDatos(await Database.BeginTransactionAsync(cancellationToken));

    public void DescartarCambios() => ChangeTracker.Clear();

    private sealed class TransaccionDatos(IDbContextTransaction transaction) : ITransaccionDatos
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

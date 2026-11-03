using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Infrastructure;

public static class RequisitosCatalogo
{
    public static IOrderedQueryable<Requisito> Publicados(this IRequisitosDatos db) => db.Requisitos
        .AsNoTracking().Include(r => r.Seccion)
        .Where(r => r.Activo && !r.Eliminado && (r.Seccion == null || r.Seccion.Activa && !r.Seccion.Eliminada))
        .OrderBy(r => r.Seccion == null ? 0 : r.Seccion.Orden).ThenBy(r => r.SeccionId)
        .ThenBy(r => r.Orden).ThenBy(r => r.Id);
}

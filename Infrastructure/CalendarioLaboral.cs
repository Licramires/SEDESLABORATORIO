using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

public sealed class CalendarioLaboral(ICatalogosDatos db) : ICalendarioLaboral
{
    public async Task<DateTime> PlazoHabil(DateTime fecha, int dias)
    {
        var fechas = await db.Catalogos.Where(c => c.Clase == "feriado" && c.Activo).Select(c => c.Nombre).ToListAsync();
        var feriados = fechas.Select(s => DateTime.ParseExact(s, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture).Date).ToHashSet();
        while (dias > 0)
        {
            fecha = fecha.AddDays(1);
            if (fecha.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !feriados.Contains(fecha.Date)) dias--;
        }
        return fecha.Date;
    }
}

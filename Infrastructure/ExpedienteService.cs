using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Infrastructure;

public record RequisitoCopia(int Id, string Nombre, bool Obligatorio, int Version, string PlantillaPdf,
    int? SeccionId = null, string SeccionNombre = "", string SeccionDescripcion = "");
public record Verificacion(int Id, string Nombre, string Referencia, int Version, string Resultado = "", string Observacion = "");
public record DatosOficiales(string Nombre, string Tipo, decimal Lat, decimal Lng);

public static class ExpedienteService
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static DatosOficiales ValidarDatos(Solicitud s)
    {
        try
        {
            using var json = JsonDocument.Parse(s.DatosLaboratorioPropuesto);
            var root = json.RootElement;
            var nombre = root.GetProperty("nombre").GetString()?.Trim();
            var tipo = root.GetProperty("tipo").GetString()?.Trim();
            var coords = root.GetProperty("coordenadas");
            var lat = coords.GetProperty("lat").GetDecimal();
            var lng = coords.GetProperty("lng").GetDecimal();
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 200 || string.IsNullOrWhiteSpace(tipo) || tipo.Length > 100 ||
                string.IsNullOrWhiteSpace(s.Municipio) || s.Municipio.Length > 100 || lat is < -90 or > 90 || lng is < -180 or > 180)
                throw new InvalidOperationException();
            return new(nombre, tipo, lat, lng);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        { throw new InvalidOperationException("Los datos oficiales del establecimiento están incompletos o dañados. No se puede avanzar hasta corregirlos."); }
    }

    public static List<RequisitoCopia> LeerRequisitos(Solicitud s)
    {
        if (string.IsNullOrEmpty(s.RequisitosJson)) return [];
        try {
            var lista = JsonSerializer.Deserialize<List<RequisitoCopia>>(s.RequisitosJson, JsonOptions);
            if (lista is null || lista.Any(r => r is null || r.Id < 1 || string.IsNullOrWhiteSpace(r.Nombre) || r.Nombre.Length > 200 || r.Version < 1) || lista.Select(r => r.Id).Distinct().Count() != lista.Count)
                throw new InvalidOperationException("La copia de requisitos está dañada; solicite su revisión administrativa.");
            return lista;
        }
        catch (JsonException) { throw new InvalidOperationException("La copia de requisitos está dañada; solicite su revisión administrativa."); }
    }

    public static async Task<List<RequisitoCopia>> Requisitos(IRequisitosDatos db, Solicitud s)
    {
        if (!string.IsNullOrEmpty(s.RequisitosJson)) return LeerRequisitos(s);
        var actuales = await db.Publicados().ToListAsync();
        return actuales.Select(r => new RequisitoCopia(r.Id, r.Nombre, r.Obligatorio, r.Version, r.PlantillaPdf,
            r.SeccionId, r.Seccion?.Nombre ?? "", r.Seccion?.Descripcion ?? "")).ToList();
    }

    public static void ComprobarDocumentos(Solicitud s, IEnumerable<RequisitoCopia> requisitos, ILectorArchivos storage, bool revisados = false)
    {
        var actuales = s.Documentos.Where(d => d.Actual).ToList();
        foreach (var r in requisitos.Where(r => r.Obligatorio))
            if (!actuales.Any(d => d.RequisitoId == r.Id && storage.Resolve(d.ArchivoPdf) != null))
                throw new InvalidOperationException($"Complete el PDF del requisito: {r.Nombre}.");
        if (revisados && actuales.Any(d => (d.EstadoRevision != "aprobado" && !(s.RequisitosReconstruidos && d.EstadoRevision == "aprobacion_global_heredada")) || storage.Resolve(d.ArchivoPdf) == null))
            throw new InvalidOperationException("Revise individualmente todos los documentos actuales antes de aprobar la documentación.");
        if (!revisados && actuales.Any(d => d.EstadoRevision == "observado"))
            throw new InvalidOperationException("Reemplace los documentos observados antes de reenviar.");
    }

    public static List<Verificacion> LeerVerificaciones(Inspeccion i)
    {
        if (string.IsNullOrEmpty(i.ListaVerificacionJson)) return [];
        try {
            var lista = JsonSerializer.Deserialize<List<Verificacion>>(i.ListaVerificacionJson, JsonOptions);
            if (lista is null || lista.Any(v => v is null || v.Id < 1 || string.IsNullOrWhiteSpace(v.Nombre) || v.Nombre.Length > 200 || v.Version < 1 || v.Resultado is not ("" or "cumple" or "observado" or "no_aplica")) || lista.Select(v => v.Id).Distinct().Count() != lista.Count)
                throw new InvalidOperationException("La copia de verificaciones está dañada; solicite su revisión administrativa.");
            return lista;
        }
        catch (JsonException) { throw new InvalidOperationException("La copia de verificaciones está dañada; solicite su revisión administrativa."); }
    }

    public static async Task<string?> ValidarCatalogos(ICatalogosDatos db, string tipo, string municipio, bool permitirSinCatalogo)
    {
        foreach (var pair in new[] { (Clase: "tipo", Valor: tipo), (Clase: "municipio", Valor: municipio) })
        {
            var items = db.Catalogos.Where(c => c.Clase == pair.Clase);
            if (!await items.AnyAsync())
            {
                if (!permitirSinCatalogo) return $"El administrador debe configurar el catálogo de {pair.Clase} antes de registrar trámites.";
            }
            else if (!await items.AnyAsync(c => c.Activo && c.Nombre == pair.Valor.Trim()))
                return $"Seleccione un {pair.Clase} activo del catálogo.";
        }
        return null;
    }

    public static bool Editable(Solicitud s) => s.Estado is EstadoSolicitud.Borrador or EstadoSolicitud.Observado;
    public static DateTime Fin(Inspeccion i) => i.FechaFin ?? i.Fecha.AddHours(1);
}

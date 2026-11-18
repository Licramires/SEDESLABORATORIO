using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;

namespace SEDESLABORATORIO.Data;

// Datos iniciales y reparación de rótulos heredados dentro del mismo modelo operativo.
public static class DemoDataSeeder
{
    private const string Marker = "demo:expedientes:v1";

    private static readonly Dictionary<string, string> Nombres = new()
    {
        ["BORRADOR"] = "Laboratorio Los Olivos", ["DOCUMENTOS"] = "Laboratorio San Rafael",
        ["ASIGNAR"] = "Laboratorio Amanecer", ["PROGRAMAR"] = "Laboratorio Los Andes",
        ["VISITA"] = "Laboratorio Primavera", ["AGENDA"] = "Laboratorio Horizonte",
        ["ACTA"] = "Laboratorio San Gabriel", ["LEGAL"] = "Laboratorio Alameda",
        ["OBSERVADO"] = "Laboratorio La Rivera", ["CITACION"] = "Laboratorio El Prado",
        ["HABILITADO"] = "Laboratorio Central", ["RENOVACION"] = "Laboratorio del Valle",
        ["RECHAZADO"] = "Laboratorio Las Flores"
    };

    // Reparación de rótulos antiguos en los mismos registros; no altera estados ni evidencias.
    public static async Task CorregirRotulosAsync(ApplicationDbContext db)
    {
        foreach (var (clase, anterior, nuevo) in new[] { ("tipo", "Clínico (DEMO)", "Clínico"), ("municipio", "Cochabamba (DEMO)", "Cochabamba") })
        {
            var item = await db.Catalogos.SingleOrDefaultAsync(c => c.Clase == clase && c.Nombre == anterior);
            if (item is null) continue;
            if (await db.Catalogos.AnyAsync(c => c.Clase == clase && c.Nombre == nuevo))
            { if (!item.Activo) continue; item.Activo = false; }
            else item.Nombre = nuevo;
            item.Version++;
        }
        var registros = await db.Solicitudes.Include(s => s.Laboratorio).ToListAsync();
        foreach (var solicitud in registros)
        {
            JsonObject? datos;
            try { datos = JsonNode.Parse(solicitud.DatosLaboratorioPropuesto) as JsonObject; }
            catch (JsonException) { continue; }
            string? anterior = null;
            if (datos?["nombre"] is JsonValue valor) valor.TryGetValue(out anterior);
            var codigoOrigen = solicitud.Codigo;
            if (solicitud.EsRenovacion && solicitud.Laboratorio?.Codigo.StartsWith("DEMO-LAB-", StringComparison.Ordinal) == true)
                codigoOrigen = "DEMO-" + solicitud.Laboratorio.Codigo[9..];
            var tareaAnterior = codigoOrigen switch {
                "DEMO-BORRADOR" => "Completar y enviar", "DEMO-DOCUMENTOS" => "Revisar documentos", "DEMO-ASIGNAR" => "Asignar supervisor",
                "DEMO-PROGRAMAR" => "Organizar fecha y hora", "DEMO-VISITA" => "Registrar acta de visita", "DEMO-AGENDA" => "Visita futura",
                "DEMO-ACTA" => "Validar acta", "DEMO-LEGAL" => "Resolver revisión legal", "DEMO-OBSERVADO" => "Subsanar documento observado",
                "DEMO-CITACION" => "Atender citación de campo", "DEMO-HABILITADO" => "Consultar licencia y servicios",
                "DEMO-RENOVACION" => "Revisar renovación", "DEMO-RECHAZADO" => "Consultar rechazo definitivo", _ => null };
            var cambio = false;
            if (tareaAnterior is not null && anterior == "DEMO - " + tareaAnterior && Nombres.TryGetValue(codigoOrigen[5..], out var nombre))
            {
                datos!["nombre"] = nombre;
                cambio = true;
                if (solicitud.Laboratorio?.Nombre == anterior) solicitud.Laboratorio.Nombre = nombre;
            }
            if (datos?["tipo"] is JsonValue tipo && tipo.TryGetValue<string>(out var textoTipo) && textoTipo == "Clínico (DEMO)")
            { datos["tipo"] = "Clínico"; cambio = true; }
            if (solicitud.Municipio == "Cochabamba (DEMO)") { solicitud.Municipio = "Cochabamba"; cambio = true; }
            if (cambio) { solicitud.DatosLaboratorioPropuesto = datos?.ToJsonString() ?? solicitud.DatosLaboratorioPropuesto; solicitud.Version++; }
        }
        foreach (var lab in await db.Laboratorios.ToListAsync())
        {
            if (lab.Servicios == "DEMO: Hematología, Química clínica") lab.Servicios = "Hematología, Química clínica";
            if (lab.Tipo == "Clínico (DEMO)") lab.Tipo = "Clínico";
            if (lab.Municipio == "Cochabamba (DEMO)") lab.Municipio = "Cochabamba";
        }
        await db.SaveChangesAsync();
    }

    public static async Task SeedAsync(ApplicationDbContext db, PdfStorage storage)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (await db.Notificaciones.AnyAsync(n => n.Clave == Marker)) return;
        var users = await db.Usuarios.Where(u => u.Email.EndsWith("@si-lab.local")).ToListAsync();
        Usuario User(string email, RolUsuario role) => users.Single(u => u.Email == email + "@si-lab.local" && u.Rol == role);
        // No cambiar roles ni credenciales de cuentas existentes para forzar una carga demo.
        if (!Enum.GetValues<RolUsuario>().All(role => users.Any(u => u.Rol == role &&
            u.Email == role.ToString().ToLowerInvariant() + "@si-lab.local"))) return;
        var owner = User("propietario", RolUsuario.Propietario);
        var coordinator = User("coordinador", RolUsuario.Coordinador);
        var supervisor = User("supervisor", RolUsuario.Supervisor);
        var lawyer = User("abogado", RolUsuario.Abogado);
        var admin = User("administrador", RolUsuario.Administrador);
        var now = DateTime.UtcNow;
        var today = TramiteService.HoraBolivia.Date;
        var files = new List<string>();
        async Task<string> Pdf(string title)
        {
            var bytes = SimplePdf.Create(["DEMOSTRACIÓN - SIN VALIDEZ OFICIAL", title,
                "Datos ficticios para probar el sistema. Las firmas de ejemplo no son firmas institucionales."]);
            using var stream = new MemoryStream(bytes);
            var key = await storage.SaveAsync(new FormFile(stream, 0, bytes.Length, "pdf", "demo.pdf"));
            files.Add(key);
            return key;
        }

        try
        {
            // Solo completar clases vacías; los catálogos ya configurados se conservan.
            foreach (var (clase, nombre, referencia) in new[] {
                ("tipo", "Clínico", "Catálogo inicial configurable"),
                ("municipio", "Cochabamba", "Catálogo inicial configurable"),
                ("inspeccion", "DEMO: condiciones de limpieza del área de trabajo", "Punto ficticio; sin artículo normativo"),
                ("formulario_acta", "DEMO: formulario de acta", "Sin validez oficial"),
                ("formulario_citacion", "DEMO: formulario de citación", "Sin validez oficial") })
            {
                if (await db.Catalogos.AnyAsync(c => c.Clase == clase)) continue;
                db.Catalogos.Add(new CatalogoItem { Clase = clase, Nombre = nombre, Referencia = referencia,
                    ArchivoPdf = clase.StartsWith("formulario_") ? await Pdf(nombre) : "" });
            }
            await db.SaveChangesAsync();
            var tipo = await db.Catalogos.Where(c => c.Clase == "tipo" && c.Activo).Select(c => c.Nombre).FirstOrDefaultAsync() ?? "Clínico";
            var municipio = await db.Catalogos.Where(c => c.Clase == "municipio" && c.Activo).Select(c => c.Nombre).FirstOrDefaultAsync() ?? "Cochabamba";
            var requirements = await db.Publicados().ToListAsync();
            var snapshot = JsonSerializer.Serialize(requirements.Select(r => new RequisitoCopia(r.Id, r.Nombre, r.Obligatorio, r.Version, r.PlantillaPdf,
                r.SeccionId, r.Seccion?.Nombre ?? "", r.Seccion?.Descripcion ?? "")));
            var checks = await db.Catalogos.Where(c => c.Clase == "inspeccion" && c.Activo).OrderBy(c => c.Id)
                .Select(c => new Verificacion(c.Id, c.Nombre, c.Referencia, c.Version, "", "")).ToListAsync();
            var template = await db.Catalogos.FirstOrDefaultAsync(c => c.Clase == "formulario_acta" && c.Activo);
            var scenarios = new[] {
                ("BORRADOR", EstadoSolicitud.Borrador, "Completar y enviar"),
                ("DOCUMENTOS", EstadoSolicitud.EnRevision, "Revisar documentos"),
                ("ASIGNAR", EstadoSolicitud.Aprobado, "Asignar supervisor"),
                ("PROGRAMAR", EstadoSolicitud.Inspeccion, "Organizar fecha y hora"),
                ("VISITA", EstadoSolicitud.Inspeccion, "Registrar acta de visita"),
                ("AGENDA", EstadoSolicitud.Inspeccion, "Visita futura"),
                ("ACTA", EstadoSolicitud.RevisionActa, "Validar acta"),
                ("LEGAL", EstadoSolicitud.RevisionLegal, "Resolver revisión legal"),
                ("OBSERVADO", EstadoSolicitud.Observado, "Subsanar documento observado"),
                ("CITACION", EstadoSolicitud.Observado, "Atender citación de campo"),
                ("HABILITADO", EstadoSolicitud.Habilitado, "Consultar licencia y servicios"),
                ("RENOVACION", EstadoSolicitud.EnRevision, "Revisar renovación"),
                ("RECHAZADO", EstadoSolicitud.Rechazado, "Consultar rechazo definitivo") };
            foreach (var (code, state, task) in scenarios)
            {
                var s = new Solicitud { Propietario = owner, Estado = state, Version = 1,
                    Codigo = "DEMO-" + code, Municipio = municipio, Direccion = "DEMO: Av. de pruebas 123",
                    Telefono = "00000000", FechaCreacion = now.AddDays(-20),
                    DatosLaboratorioPropuesto = JsonSerializer.Serialize(new { nombre = Nombres[code],
                        tipo, coordenadas = new { lat = -17.3935m, lng = -66.1570m } }) };
                void History(Usuario actor, string detail, int days) => s.Historial.Add(new HistorialTramite {
                    Actor = actor, Fecha = now.AddDays(days), Detalle = "DEMO: " + detail });
                History(owner, "Borrador creado con datos ficticios.", -20);
                if (state != EstadoSolicitud.Borrador)
                {
                    s.PrimerEnvio = s.FechaEnvio = now.AddDays(-18);
                    s.RequisitosJson = snapshot;
                    History(owner, "Solicitud enviada para revisión documental.", -18);
                }
                var reviewed = state is not (EstadoSolicitud.Borrador or EstadoSolicitud.EnRevision or EstadoSolicitud.Rechazado) && code != "OBSERVADO";
                foreach (var r in requirements)
                    s.Documentos.Add(new Documento { RequisitoId = r.Id, NombreRequisito = r.Nombre,
                        ArchivoPdf = await Pdf($"{s.Codigo}: {r.Nombre}"), FechaCarga = now.AddDays(-19),
                        EstadoRevision = reviewed ? "aprobado" : "pendiente",
                        RevisorId = reviewed ? coordinator.Id : null, FechaRevision = reviewed ? now.AddDays(-17) : null });
                if (reviewed) History(coordinator, "Documentos revisados y aprobados individualmente.", -17);
                if (code == "OBSERVADO")
                {
                    s.OrigenObservacion = "documental";
                    if (s.Documentos.FirstOrDefault() is { } d)
                    {
                        d.EstadoRevision = "observado"; d.Observacion = "DEMO: adjunte una copia legible para subsanar.";
                        d.RevisorId = coordinator.Id; d.FechaRevision = now.AddDays(-2);
                    }
                    History(coordinator, "Documentación observada: adjunte una copia legible.", -2);
                }
                if (code is "PROGRAMAR" or "VISITA" or "AGENDA" or "ACTA" or "LEGAL" or "CITACION" or "HABILITADO")
                {
                    var complete = code is "ACTA" or "LEGAL" or "CITACION" or "HABILITADO";
                    var observed = code == "CITACION";
                    var date = complete ? today.AddDays(-11).AddHours(code == "ACTA" ? 9 : code == "LEGAL" ? 11 : code == "CITACION" ? 13 : 15) :
                        code == "AGENDA" ? today.AddDays(2).AddHours(10) : TramiteService.HoraBolivia.AddHours(-2);
                    var i = new Inspeccion { Supervisor = supervisor, Fecha = code == "PROGRAMAR" ? default : date,
                        FechaFin = code == "PROGRAMAR" ? null : date.AddHours(1), Programada = code != "PROGRAMAR",
                        Estado = complete ? code == "ACTA" ? "registrada" : "validada" : "pendiente",
                        Resultado = complete ? observed ? "observado" : "aprobado" : "",
                        Acta = complete ? observed ? "DEMO: mejorar limpieza del área de trabajo." : "DEMO: puntos verificados conformes." : "",
                        ActaPdf = complete ? await Pdf($"{s.Codigo}: acta de inspección de ejemplo") : "",
                        ListaVerificacionJson = JsonSerializer.Serialize(checks.Select(c => c with {
                            Resultado = complete ? observed ? "observado" : "cumple" : "",
                            Observacion = observed ? "DEMO: mejorar limpieza del área de trabajo." : "" })),
                        PlantillaPdf = template?.ArchivoPdf ?? "", PlantillaVersion = template?.Version ?? 0,
                        PlazoSubsanacion = observed ? today.AddDays(7) : null };
                    s.Inspecciones.Add(i);
                    History(coordinator, "Inspección asignada al Supervisor Demo.", -16);
                    if (complete) History(supervisor, "Acta registrada: " + i.Resultado + ".", -10);
                    if (complete && code != "ACTA") History(coordinator, "Acta validada: " + i.Resultado + ".", -9);
                    if (observed)
                    {
                        s.OrigenObservacion = "campo";
                        i.Citacion = "DEMO: subsanar limpieza del área de trabajo.";
                        s.Citaciones.Add(new Citacion { Inspeccion = i, SupervisorId = supervisor.Id,
                            FechaEmision = today.AddDays(-2), Vencimiento = today.AddDays(7), AnticipacionDias = 5,
                            Motivo = i.Citacion, EvidenciaPdf = await Pdf("DEMO: citación entregada; sin validez oficial") });
                        History(supervisor, "Citación emitida para subsanar limpieza del área de trabajo.", -2);
                    }
                }
                if (code is "HABILITADO" or "RENOVACION")
                {
                    s.Laboratorio = new Laboratorio { Nombre = ExpedienteService.ValidarDatos(s).Nombre,
                        Tipo = tipo, Municipio = municipio, Lat = -17.3935m, Lng = -66.1570m, Propietario = owner,
                        Direccion = s.Direccion, Telefono = s.Telefono, Codigo = "DEMO-LAB-" + code,
                        DatosVerificados = true, Estado = EstadoLaboratorio.Abierto,
                        VigenciaHasta = code == "HABILITADO" ? today.AddDays(-5).AddYears(1) : today.AddDays(15), Servicios = "Hematología, Química clínica" };
                    if (code == "RENOVACION") s.EsRenovacion = true;
                    else
                    {
                        s.FechaCierre = now.AddDays(-5); s.VigenciaOtorgadaDesde = today.AddDays(-5);
                        s.VigenciaOtorgadaHasta = s.Laboratorio.VigenciaHasta;
                        s.FirmaLegalPdf = await Pdf("DEMO: resolución legal y firma ficticia");
                        History(lawyer, "Habilitación legal de demostración; sin validez oficial.", -5);
                    }
                }
                if (code == "RECHAZADO")
                {
                    s.MotivoRechazo = "documental"; s.FechaCierre = now.AddDays(-3);
                    History(coordinator, "Rechazo definitivo de ejemplo: documentación inválida.", -3);
                }
                db.Solicitudes.Add(s);
            }
            await db.SaveChangesAsync();
            foreach (var s in db.ChangeTracker.Entries<Solicitud>().Select(e => e.Entity).Where(s => s.Estado == EstadoSolicitud.Habilitado))
                s.InformeCierrePdf = InformeTramite.Crear(s, s.Historial);
            foreach (var user in users.Where(u => Enum.GetValues<RolUsuario>().Any(role =>
                u.Rol == role && u.Email == role.ToString().ToLowerInvariant() + "@si-lab.local")))
            {
                var task = user.Rol switch {
                    RolUsuario.Propietario => "Pruebe el borrador, la subsanación, el seguimiento, sus laboratorios y la renovación.",
                    RolUsuario.Coordinador => "Tiene documentos y actas por revisar, y un expediente aprobado para asignar.",
                    RolUsuario.Supervisor => "Tiene una visita por programar, una lista para registrar acta, otra futura y una citación.",
                    RolUsuario.Abogado => "Tiene un expediente completo en revisión legal para aprobar, observar o rechazar.",
                    RolUsuario.Gerente => "Consulte métricas: aperturas, renovación, habilitación, rechazo y actividad por etapa.",
                    _ => "Pruebe usuarios, permisos, requisitos y catálogos; los formularios DEMO no tienen validez oficial." };
                db.Notificaciones.Add(new Notificacion { UsuarioId = user.Id, Fecha = now,
                    Mensaje = "DEMO: " + task, Clave = user.Id == admin.Id ? Marker : $"{Marker}:{user.Id}" });
            }
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            foreach (var file in files) storage.Delete(file);
            throw;
        }
    }
}

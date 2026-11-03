using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class WorkflowTests
{
    [Fact]
    public async Task Migration_preserves_october_documents_and_explains_reconstructed_history()
    {
        await using var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options);
        var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync("20261012174533_RolePermissions");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Usuarios (Nombre,Email,Password,Rol,Activo,SelloSeguridad) VALUES ('Anterior','anterior-migracion@example.test','hash','propietario',1,'sello')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Requisitos (Nombre,Obligatorio,Activo) VALUES ('Licencia anterior',1,1)");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Solicitudes (PropietarioId,DatosLaboratorioPropuesto,Estado,FechaCreacion,Version,Municipio,EsRenovacion,FirmaLegalPdf) VALUES (1,'{{}}','aprobado','2026-10-01',2,'Sacaba',0,'')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Documentos (SolicitudId,RequisitoId,ArchivoPdf) VALUES (1,1,'primero.pdf'),(1,1,'segundo.pdf'),(1,0,'complemento.pdf')");
        await migrator.MigrateAsync();
        var s = await db.Solicitudes.Include(s => s.Documentos).SingleAsync(); Assert.Equal(EstadoSolicitud.Aprobado, s.Estado); Assert.True(s.RequisitosReconstruidos);
        Assert.Equal("Licencia anterior", Assert.Single(ExpedienteService.LeerRequisitos(s)).Nombre);
        var docs = s.Documentos.Where(d => d.RequisitoId == 1).OrderBy(d => d.VersionArchivo).ToList(); Assert.Equal(2, docs.Count); Assert.False(docs[0].Actual); Assert.True(docs[1].Actual);
        Assert.Equal("aprobacion_global_heredada", docs[1].EstadoRevision); Assert.Null(docs[1].FechaRevision); Assert.Null(docs[1].FechaCarga); Assert.Null(docs[1].RevisorId);
        Assert.Equal("segundo.pdf", docs[1].ArchivoPdf); Assert.True(s.Documentos.Single(d => d.RequisitoId == 0).Actual);
        Assert.Null(s.VigenciaOtorgadaHasta); Assert.Null(s.FechaCierre); Assert.Null(s.PrimerEnvio);
        var pdf = Encoding.Latin1.GetString(InformeTramite.Crear(s,s.Historial)); Assert.Contains("Vigencia histórica no registrada",pdf);
    }

    [Fact]
    public async Task Inspection_checklist_and_template_are_frozen_and_can_be_prepared_before_signing()
    {
        using var owner = await Login("propietario"); using var coordinator = await Login("coordinador"); using var supervisor = await Login("supervisor");
        await using var scope = app.Services.CreateAsyncScope(); var storage = scope.ServiceProvider.GetRequiredService<PdfStorage>();
        using var stream = new MemoryStream(Pdf); var key = await storage.SaveAsync(new FormFile(stream,0,Pdf.Length,"Plantilla","plantilla.pdf"));
        var catIds = await app.Db(async db => { var c = new CatalogoItem { Clase = "inspeccion", Nombre = "Punto de prueba", Referencia = "Referencia de ensayo", Version = 1 }; var t = new CatalogoItem { Clase = "formulario_acta", Nombre = "Plantilla de ensayo", ArchivoPdf = key }; db.AddRange(c,t); await db.SaveChangesAsync(); return new[] {c.Id,t.Id}; });
        try
        {
            var id = await Ready(owner,coordinator,"Acta estructurada"); var supervisorId = await app.Db(db => db.Usuarios.Where(u => u.Rol == RolUsuario.Supervisor).Select(u => u.Id).FirstAsync());
            await Decide(coordinator,id,"asignar",new() { ["SupervisorId"] = supervisorId.ToString() });
            await app.Db(async db => { var i = await db.Inspecciones.SingleAsync(i => i.SolicitudId == id); i.Programada=true; i.Fecha=TramiteService.HoraBolivia.AddMinutes(-10); var c = await db.Catalogos.FindAsync(catIds[0]); c!.Nombre="Nombre posterior del catálogo"; c.Version++; return await db.SaveChangesAsync(); });
            var checks = await app.Db(db => db.Inspecciones.Where(i=>i.SolicitudId==id).Select(i=>i.ListaVerificacionJson).SingleAsync()); Assert.Contains("Punto de prueba",checks); Assert.DoesNotContain("Nombre posterior",checks);
            var fields = new Dictionary<string,string> { ["Resultado"]="observado", ["DiasSubsanacion"]="5", ["Verificaciones[0].Id"]=catIds[0].ToString(), ["Verificaciones[0].Resultado"]="observado", ["Verificaciones[0].Observacion"]="Falta evidencia" };
            var conformes = new Dictionary<string,string>(fields) { ["Resultado"]="aprobado", ["Verificaciones[0].Resultado"]="cumple", ["Verificaciones[0].Observacion"]="" };
            var before = await Version(id); await Decide(supervisor,id,"preparar_acta",conformes); Assert.True(await Version(id) > before);
            await Decide(supervisor,id,"preparar_acta",new(fields)); Assert.Equal(EstadoSolicitud.Inspeccion,await State(id));
            var summary = Encoding.Latin1.GetString(await supervisor.GetByteArrayAsync($"/Gestion/FormularioActa/{id}?resumen=true")); Assert.Contains("Punto de prueba",summary); Assert.Contains("Falta evidencia",summary);
            Assert.Equal(Pdf,await supervisor.GetByteArrayAsync($"/Gestion/FormularioActa/{id}"));
            fields["Resultado"]="aprobado"; await Decide(supervisor,id,"acta",new(fields),Pdf); Assert.Equal(EstadoSolicitud.Inspeccion,await State(id));
            fields["Resultado"]="observado"; await Decide(supervisor,id,"acta",fields,Pdf); Assert.Equal(EstadoSolicitud.RevisionActa,await State(id));
            Assert.Equal(5,await app.Db(db=>db.Inspecciones.Where(i=>i.SolicitudId==id).Select(i=>i.DiasSubsanacion).SingleAsync()));
        }
        finally { await app.Db(async db => { db.Catalogos.RemoveRange(await db.Catalogos.Where(c=>catIds.Contains(c.Id)).ToListAsync()); return await db.SaveChangesAsync(); }); }
    }

    [Fact]
    public async Task Business_calendar_uses_only_configured_holidays()
    {
        var id = await app.Db(async db => { var c = new CatalogoItem { Clase="feriado",Nombre="2026-10-12",Referencia="Feriado de prueba" }; db.Add(c); await db.SaveChangesAsync(); return c.Id; });
        try { Assert.Equal(new DateTime(2026,10,14), await app.Db(db=>new TramiteService(db).PlazoHabil(new DateTime(2026,10,9),2))); }
        finally { await app.Db(async db => { db.Catalogos.Remove((await db.Catalogos.FindAsync(id))!); return await db.SaveChangesAsync(); }); }
    }

    [Fact]
    public async Task Login_rate_limit_still_blocks_repeated_attempts_from_same_client()
    {
        using var c=app.Browser();var token=await Token(c,"/auth/Auth/Login");
        HttpResponseMessage? last=null;
        for(var i=0;i<31;i++) { last?.Dispose(); last=await c.PostAsync("/auth/Auth/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","no-existe@example.test"},{"Password","incorrecta"},{"__RequestVerificationToken",token}})); }
        Assert.Equal(HttpStatusCode.TooManyRequests,last!.StatusCode);last.Dispose();
    }

    [Fact]
    public async Task Browser_can_submit_empty_optional_contact_fields()
    {
        using var owner=await Login("propietario");
        var r=await Post(owner,"/solicitud-registro/SolicitudRegistro/Nueva","/solicitud-registro/SolicitudRegistro/Nueva",new() { ["NombreLaboratorio"]="Datos opcionales vacíos",["TipoLaboratorio"]="Clínico",["Municipio"]="Sacaba",["Latitud"]="-17.39",["Longitud"]="-66.15",["Direccion"]="",["Telefono"]="",["Codigo"]="" });
        Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);
    }

    [Theory]
    [InlineData("/Gestion/Documento",12)]
    [InlineData("/solicitud-registro/SolicitudRegistro/Nueva",56)]
    public async Task Oversize_content_length_is_rejected_before_binding(string path,int megabytes)
    {
        using var c=app.Browser();using var content=new ByteArrayContent([]);content.Headers.ContentLength=megabytes*1024L*1024;
        using var r=await c.PostAsync(path,content);Assert.Equal(HttpStatusCode.RequestEntityTooLarge,r.StatusCode);
        Assert.Contains("supera",await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Production_hides_unverified_laboratories_and_does_not_seed_unofficial_requirements()
    {
        var directory=Path.Combine(Path.GetTempPath(),"silab-production-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        using var production=app.WithWebHostBuilder(builder=>{
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?> {
                ["ConnectionStrings:DefaultConnection"]=$"Data Source={Path.Combine(directory,"production.db")}", ["Storage:Root"]=Path.Combine(directory,"documents"),
                ["Bootstrap:AdminEmail"]="admin@example.test",["Bootstrap:AdminPassword"]="SoloPruebas123!",
                ["Auth:SeedDemoUsers"]="true",["Auth:SeedDemoData"]="true"
            }));
        });
        using var client=production.CreateClient(new WebApplicationFactoryClientOptions {BaseAddress=new Uri("https://localhost")});
        await using(var scope=production.Services.CreateAsyncScope()) {
            var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();Assert.Empty(await db.Requisitos.ToListAsync());
            Assert.Empty(await db.Solicitudes.ToListAsync()); Assert.Empty(await db.Catalogos.ToListAsync());
            Assert.DoesNotContain(await db.Usuarios.ToListAsync(), u => u.Email.EndsWith("@si-lab.local"));
            var owner=new Usuario {Nombre="Propietario de prueba",Email="owner-production@example.test",Password="hash-no-usado",Rol=RolUsuario.Propietario};db.Add(owner);await db.SaveChangesAsync();
            db.AddRange(new Laboratorio {Nombre="No verificado de prueba",Tipo="Clínico",Estado=EstadoLaboratorio.Abierto},new Laboratorio {Nombre="Verificado de prueba",Tipo="Clínico",Estado=EstadoLaboratorio.Abierto,DatosVerificados=true,PropietarioId=owner.Id,VigenciaHasta=DateTime.UtcNow.AddDays(20)});await db.SaveChangesAsync();
        }
        var html=WebUtility.HtmlDecode(await client.GetStringAsync("/laboratorios"));Assert.DoesNotContain("No verificado de prueba",html);Assert.Contains("Verificado de prueba",html);
    }

    [Theory]
    [InlineData("falso.png")]
    [InlineData("incompleto.jpg")]
    public async Task Citation_images_reject_invalid_contents(string nombre)
    {
        var data=Encoding.ASCII.GetBytes("contenido que no corresponde a una imagen");using var stream=new MemoryStream(data);
        Assert.NotNull(await EvidenciaArchivo.Validar(new FormFile(stream,0,data.Length,"Evidencia",nombre)));
    }
}

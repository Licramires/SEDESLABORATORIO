using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public sealed class ExhaustiveFixture : IDisposable
{
    public TestApplication App { get; } = new() { SeedDemoData = true };
    private readonly Dictionary<string,(HttpClient Client,string Token)> clients = [];
    public async Task<(HttpClient Client,string Token)> Client(string role)
    {
        if (clients.TryGetValue(role,out var saved)) return saved;
        var c=App.Browser(); var html=await c.GetStringAsync("/auth/Auth/Login");
        string Token(string h)=>WebUtility.HtmlDecode(Regex.Match(h,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        if(role!="anonimo") {
            var response=await c.PostAsync("/auth/Auth/Login",new FormUrlEncodedContent(new Dictionary<string,string> {
                ["Email"]=role+"@si-lab.local",["Password"]="Demo123!",["__RequestVerificationToken"]=Token(html) }));
            Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
            html=await c.GetStringAsync("/auth/Auth/Dashboard");
        }
        saved=(c,Token(html)); clients.Add(role,saved); return saved;
    }
    public async Task<HttpResponseMessage> Post(string role,string route,Dictionary<string,string> fields,byte[]? file=null,string fileField="Pdf",string fileName="firma.pdf",bool csrf=true)
    {
        var (client,token)=await Client(role);
        using var form=new MultipartFormDataContent();
        foreach(var p in fields) form.Add(new StringContent(p.Value),p.Key);
        if(csrf)form.Add(new StringContent(token),"__RequestVerificationToken");
        if(file!=null)form.Add(new ByteArrayContent(file),fileField,fileName);
        return await client.PostAsync(route,form);
    }
    public async Task<Solicitud> Case(EstadoSolicitud state,bool observedDoc=false)
        =>await App.Db(async db=>{
            var source=await db.Solicitudes.Include(s=>s.Documentos).Include(s=>s.Inspecciones).SingleAsync(s=>s.Codigo=="DEMO-LEGAL");
            var owner=await db.Usuarios.SingleAsync(u=>u.Email=="propietario@si-lab.local");
            var supervisor=await db.Usuarios.SingleAsync(u=>u.Email=="supervisor@si-lab.local");
            var previous=source.Inspecciones.Single();
            var s=new Solicitud { PropietarioId=owner.Id,DatosLaboratorioPropuesto=source.DatosLaboratorioPropuesto,Municipio=source.Municipio,
                Direccion=source.Direccion,Telefono=source.Telefono,Estado=state,Version=1,FechaCreacion=DateTime.UtcNow.AddDays(-10),
                RequisitosJson=source.RequisitosJson,PrimerEnvio=state==EstadoSolicitud.Borrador?null:DateTime.UtcNow.AddDays(-9),
                OrigenObservacion=state==EstadoSolicitud.Observado?"campo":"",MotivoRechazo=state==EstadoSolicitud.Rechazado?"campo":"" };
            foreach(var d in source.Documentos.Where(d=>d.Actual))s.Documentos.Add(new Documento { RequisitoId=d.RequisitoId,NombreRequisito=d.NombreRequisito,ArchivoPdf=d.ArchivoPdf,EstadoRevision="aprobado",VersionArchivo=1 });
            if(observedDoc)s.Documentos.First().EstadoRevision="observado";
            s.Inspecciones.Add(new Inspeccion { SupervisorId=supervisor.Id,Fecha=TramiteService.HoraBolivia.AddHours(-2),FechaFin=TramiteService.HoraBolivia.AddHours(-1),Programada=true,
                Estado=state==EstadoSolicitud.RevisionActa?"registrada":state is EstadoSolicitud.RevisionLegal or EstadoSolicitud.Habilitado or EstadoSolicitud.Observado or EstadoSolicitud.Rechazado?"validada":state==EstadoSolicitud.Inspeccion?"pendiente":"reprogramada",
                Resultado=state==EstadoSolicitud.Observado?"observado":state==EstadoSolicitud.Rechazado?"rechazado":"aprobado",Acta="Verificación documentada",ActaPdf=previous.ActaPdf,
                ListaVerificacionJson=previous.ListaVerificacionJson,PlantillaPdf=previous.PlantillaPdf,PlantillaVersion=previous.PlantillaVersion });
            db.Add(s); await db.SaveChangesAsync(); return s;
        });
    public static void Report(string group,object data)
    {
        var directory=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../TestResults/exhaustiva"));
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory,group+".json"),JsonSerializer.Serialize(data,new JsonSerializerOptions { WriteIndented=true }));
    }
    public void Dispose(){foreach(var c in clients.Values)c.Client.Dispose();App.Dispose();}
}

public partial class ComprehensiveAuditTests(ExhaustiveFixture audit) : IClassFixture<ExhaustiveFixture>
{
    private static readonly string[] Roles=["propietario","coordinador","supervisor","abogado","gerente","administrador"];
    private static readonly byte[] Pdf=SimplePdf.Create(["Prueba automatizada del flujo", "Archivo sintético"]);
    private sealed record Transition(string Action,string Role,EstadoSolicitud[] From,EstadoSolicitud To);
    private static readonly Transition[] Transitions=[
        new("enviar","propietario",[EstadoSolicitud.Borrador,EstadoSolicitud.Observado],EstadoSolicitud.EnRevision),
        new("aprobar_documentos","coordinador",[EstadoSolicitud.EnRevision],EstadoSolicitud.Aprobado),
        new("observar_documentos","coordinador",[EstadoSolicitud.EnRevision],EstadoSolicitud.Observado),
        new("rechazar_documentos","coordinador",[EstadoSolicitud.EnRevision],EstadoSolicitud.Rechazado),
        new("asignar","coordinador",[EstadoSolicitud.Aprobado,EstadoSolicitud.Inspeccion,EstadoSolicitud.Observado,EstadoSolicitud.Rechazado],EstadoSolicitud.Inspeccion),
        new("programar","supervisor",[EstadoSolicitud.Inspeccion],EstadoSolicitud.Inspeccion),
        new("preparar_acta","supervisor",[EstadoSolicitud.Inspeccion],EstadoSolicitud.Inspeccion),
        new("acta","supervisor",[EstadoSolicitud.Inspeccion],EstadoSolicitud.RevisionActa),
        new("aprobar_acta","coordinador",[EstadoSolicitud.RevisionActa],EstadoSolicitud.RevisionLegal),
        new("rechazar_acta","coordinador",[EstadoSolicitud.RevisionActa],EstadoSolicitud.Aprobado),
        new("aprobar_legal","abogado",[EstadoSolicitud.RevisionLegal],EstadoSolicitud.Habilitado),
        new("observar_legal","abogado",[EstadoSolicitud.RevisionLegal],EstadoSolicitud.Observado),
        new("rechazar_legal","abogado",[EstadoSolicitud.RevisionLegal],EstadoSolicitud.Rechazado)
    ];
    private static Dictionary<string,string> Fields(Solicitud s,string action)=>new() {
        ["Id"]=s.Id.ToString(),["Version"]=s.Version.ToString(),["Accion"]=action,["Motivo"]="Fundamento concreto del control de prueba",
        ["Resultado"]="aprobado",["SupervisorId"]=s.Inspecciones.Single().SupervisorId.ToString(),
        ["Fecha"]=TramiteService.HoraBolivia.Date.AddDays(100+s.Id).AddHours(9).ToString("s"),
        ["FechaFin"]=TramiteService.HoraBolivia.Date.AddDays(100+s.Id).AddHours(10).ToString("s") };

    [Fact]
    public async Task Every_role_state_action_combination_denies_forbidden_transition_without_side_effects()
    {
        var result=new List<object>();
        foreach(var state in Enum.GetValues<EstadoSolicitud>())
        {
            var s=await audit.Case(state);
            foreach(var t in Transitions)foreach(var role in Roles)
            {
                if(role==t.Role && t.From.Contains(state))continue;
                using var response=await audit.Post(role,"/Gestion/Decidir",Fields(s,t.Action),Pdf);
                Assert.True((int)response.StatusCode<500,$"{role}/{state}/{t.Action}: {response.StatusCode}");
                await audit.App.Db(async db=>{var actual=await db.Solicitudes.SingleAsync(x=>x.Id==s.Id);
                    Assert.Equal(state,actual.Estado); Assert.Equal(s.Version,actual.Version);
                    Assert.Equal(0,await db.HistorialTramites.CountAsync(h=>h.SolicitudId==s.Id));return 0;});
                result.Add(new { Role=role,State=state.ToString(),t.Action,Status=(int)response.StatusCode,Preserved=true });
            }
        }
        Assert.Equal(685,result.Count);
        ExhaustiveFixture.Report("transiciones-prohibidas",result);
    }

    [Fact]
    public async Task Every_declared_valid_transition_executes_its_expected_effect()
    {
        var result=new List<object>();
        foreach(var t in Transitions)foreach(var state in t.From)
        {
            var s=await audit.Case(state,t.Action=="observar_documentos");var fields=Fields(s,t.Action);
            var checks=ExpedienteService.LeerVerificaciones(s.Inspecciones.Single());
            for(var n=0;n<checks.Count;n++){fields[$"Verificaciones[{n}].Id"]=checks[n].Id.ToString();fields[$"Verificaciones[{n}].Resultado"]="cumple";}
            if(t.Action=="observar_legal")fields["DocumentosObservados[0]"]=s.Documentos.First().Id.ToString();
            using var response=await audit.Post(t.Role,"/Gestion/Decidir",fields,Pdf);
            Assert.True(response.StatusCode==HttpStatusCode.Redirect,$"{t.Action}/{state}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            await audit.App.Db(async db=>{var actual=await db.Solicitudes.SingleAsync(x=>x.Id==s.Id);
                Assert.Equal(t.To,actual.Estado);Assert.True(actual.Version>s.Version);
                Assert.Equal(1,await db.HistorialTramites.CountAsync(h=>h.SolicitudId==s.Id));
                if(t.Action=="aprobar_legal"){Assert.NotNull(actual.InformeCierrePdf);Assert.NotNull(actual.LaboratorioId);}
                return 0;});
            var fingerprint=await Fingerprint();
            using(var repeated=await audit.Post(t.Role,"/Gestion/Decidir",fields,Pdf))
            {
                Assert.True((int)repeated.StatusCode<500,$"Reenvío {t.Action}: {(int)repeated.StatusCode}");
                Assert.Equal(fingerprint,await Fingerprint());
            }
            result.Add(new { t.Action,t.Role,From=state.ToString(),To=t.To.ToString(),Executed=true });
        }
        Assert.Equal(17,result.Count);
        ExhaustiveFixture.Report("transiciones-validas",result);
    }

    [Theory]
    [InlineData("/Administracion/EstadoRequisito","activo")]
    [InlineData("/Administracion/EstadoSeccion","activa")]
    [InlineData("/Administracion/Permiso","habilitado")]
    public async Task Malformed_boolean_cannot_silently_change_a_setting(string route,string field)
    {
        var ids=await audit.App.Db(async db=>{var req=new Requisito { Nombre="Control "+Guid.NewGuid(),Activo=true };var section=new SeccionRequisito { Nombre="Control "+Guid.NewGuid(),Activa=true };db.AddRange(req,section);await db.SaveChangesAsync();return(req.Id,SectionId:section.Id);});
        var fields=new Dictionary<string,string> { ["id"]=(field=="activa"?ids.SectionId:ids.Id).ToString(),["version"]="1",["rol"]="Propietario",["clave"]="solicitudes",[field]="valor-inválido" };
        using var response=await audit.Post("administrador",route,fields);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        await audit.App.Db(async db=>{Assert.True((await db.Requisitos.FindAsync(ids.Id))!.Activo);Assert.True((await db.SeccionesRequisitos.FindAsync(ids.SectionId))!.Activa);
            Assert.False(await db.PermisosRol.AnyAsync(p=>p.Rol==RolUsuario.Propietario&&p.Clave=="solicitudes"&&!p.Habilitado));return 0;});
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("[{\"Id\":1,\"Nombre\":null,\"Version\":1}]")]
    [InlineData("[{\"Id\":1,\"Nombre\":\"A\",\"Version\":1},{\"Id\":1,\"Nombre\":\"B\",\"Version\":1}]")]
    public async Task Structurally_damaged_snapshots_are_not_treated_as_valid_data(string json)
    {
        Assert.Throws<InvalidOperationException>(()=>ExpedienteService.LeerRequisitos(new Solicitud { RequisitosJson=json }));
        Assert.Throws<InvalidOperationException>(()=>ExpedienteService.LeerVerificaciones(new Inspeccion { ListaVerificacionJson=json }));
        await Task.CompletedTask;
    }
}

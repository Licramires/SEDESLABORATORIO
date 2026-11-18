using System.Net;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class ComprehensiveAuditTests
{
    [Fact]
    public async Task Dedicated_prepare_action_chooses_its_own_operation_and_legacy_send_only_redirects()
    {
        var payload=await Form("acta");payload.Fields.Remove("Accion");
        using var prepared=await audit.Post("supervisor","/Gestion/PrepararActa",payload.Fields);
        Assert.Equal(HttpStatusCode.Redirect,prepared.StatusCode);
        var id=int.Parse(payload.Fields["Id"]);
        await audit.App.Db(async db=>{
            var s=await db.Solicitudes.Include(x=>x.Inspecciones).SingleAsync(s=>s.Id==id);
            Assert.Equal(EstadoSolicitud.Inspeccion,s.Estado);Assert.Equal(2,s.Version);Assert.NotEmpty(s.Inspecciones.Single().Acta);return 0;
        });
        var draft=await audit.Case(EstadoSolicitud.Borrador);await audit.Client("propietario");var before=await Fingerprint();
        using var legacy=await audit.Post("propietario","/solicitud-seguimiento/SolicitudSeguimiento/Enviar",new() { ["id"]=draft.Id.ToString() });
        Assert.Equal(HttpStatusCode.Redirect,legacy.StatusCode);Assert.Equal($"/Gestion/Detalle/{draft.Id}",legacy.Headers.Location?.ToString());Assert.Equal(before,await Fingerprint());
    }

    [Theory]
    [InlineData("vigencia-extrema")][InlineData("propietario-distinto")][InlineData("renovacion-sin-laboratorio")]
    public async Task Legal_approval_blocks_inconsistent_renewal_links_and_impossible_extension_without_mutating_any_table(string problem)
    {
        var s=await audit.Case(EstadoSolicitud.RevisionLegal);
        await audit.App.Db(async db=>{
            var current=await db.Solicitudes.FindAsync(s.Id);current!.EsRenovacion=true;
            if(problem!="renovacion-sin-laboratorio")
            {
                var owner=problem=="propietario-distinto"?await db.Usuarios.Where(u=>u.Email=="administrador@si-lab.local").Select(u=>u.Id).SingleAsync():s.PropietarioId;
                current.Laboratorio=new Laboratorio { Nombre="Laboratorio de renovación",Tipo="Clínico",Municipio="Cochabamba",Lat=-17,Lng=-66,PropietarioId=owner,DatosVerificados=true,VigenciaHasta=problem=="vigencia-extrema"?DateTime.MaxValue:DateTime.UtcNow.AddMonths(1) };
            }
            await db.SaveChangesAsync();return 0;
        });
        await audit.Client("abogado");var before=await Fingerprint();
        using var response=await audit.Post("abogado","/Gestion/Decidir",Fields(s,"aprobar_legal"),Pdf);
        Assert.True((int)response.StatusCode<500,$"{problem}: {(int)response.StatusCode}");Assert.Equal(before,await Fingerprint());
    }

    [Fact]
    public async Task Document_return_reuses_individual_correction_notes_without_requiring_them_twice()
    {
        var s=await audit.Case(EstadoSolicitud.EnRevision,true);
        var note="Adjuntar la página que falta.";
        await audit.App.Db(async db=>{var doc=await db.Documentos.FindAsync(s.Documentos.First().Id);doc!.Observacion=note;await db.SaveChangesAsync();return 0;});
        var fields=Fields(s,"observar_documentos");fields.Remove("Motivo");
        using var response=await audit.Post("coordinador","/Gestion/Decidir",fields);
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        await audit.App.Db(async db=>{Assert.Equal(EstadoSolicitud.Observado,(await db.Solicitudes.FindAsync(s.Id))!.Estado);Assert.Contains(note,(await db.HistorialTramites.SingleAsync(h=>h.SolicitudId==s.Id)).Detalle);return 0;});
        var (owner,_)=await audit.Client("propietario");
        var sent=await audit.Case(EstadoSolicitud.EnRevision);
        Assert.DoesNotContain("Agregue PDF uno por uno",await owner.GetStringAsync($"/Gestion/Detalle/{sent.Id}"));
    }

    [Theory]
    [InlineData("requisito")][InlineData("catalogo")][InlineData("acta")][InlineData("citacion")][InlineData("nueva")]
    public async Task All_file_forms_reject_empty_wrong_extension_signature_and_truncated_pdf(string name)
    {
        var payload=await Form(name);
        var field=name switch { "requisito" or "catalogo"=>"Plantilla","nueva"=>"DocumentosPdf",_=>payload.FileField };
        foreach(var (bytes,filename) in new[] { (Array.Empty<byte>(),"vacío.pdf"),(Pdf,"archivo.txt"),(System.Text.Encoding.UTF8.GetBytes("Texto disfrazado"),"falso.pdf"),(System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nTruncado"),"incompleto.pdf") })
        {
            await audit.Client(payload.Role);var before=await Fingerprint();
            using var r=await audit.Post(payload.Role,payload.Route,payload.Fields,bytes,field,filename);
            Assert.True((int)r.StatusCode<500,$"{name}/{filename}: {(int)r.StatusCode}");Assert.Equal(before,await Fingerprint());
        }
    }

    [Fact]
    public async Task Services_reject_an_old_tab_instead_of_overwriting_the_last_saved_text()
    {
        var lab=await audit.App.Db(db=>db.Laboratorios.SingleAsync(l=>l.Codigo=="DEMO-LAB-HABILITADO"));
        var version=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(lab.Servicios)));
        var first="Servicios corregidos "+Guid.NewGuid();
        using var saved=await audit.Post("propietario","/Gestion/Servicios",new() { ["id"]=lab.Id.ToString(),["version"]=version,["servicios"]=first });
        Assert.Equal(HttpStatusCode.Redirect,saved.StatusCode);
        using var stale=await audit.Post("propietario","/Gestion/Servicios",new() { ["id"]=lab.Id.ToString(),["version"]=version,["servicios"]="Texto antiguo de otra pestaña" });
        Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        Assert.Equal(first,await audit.App.Db(db=>db.Laboratorios.Where(l=>l.Id==lab.Id).Select(l=>l.Servicios).SingleAsync()));
    }

    [Fact]
    public async Task Every_role_can_mark_its_own_notification_and_cannot_mark_someone_elses()
    {
        foreach(var role in Roles)
        {
            var notification=await audit.App.Db(async db=>{
                var user=await db.Usuarios.SingleAsync(u=>u.Email==role+"@si-lab.local");
                var n=new Notificacion { UsuarioId=user.Id,Mensaje="Prueba de lectura",Fecha=DateTime.UtcNow };db.Add(n);await db.SaveChangesAsync();return n;
            });
            var other=role=="propietario"?"gerente":"propietario";
            using var denied=await audit.Post(other,"/Notificaciones/Leer",new() { ["id"]=notification.Id.ToString() });
            Assert.Equal(HttpStatusCode.NotFound,denied.StatusCode);
            using var saved=await audit.Post(role,"/Notificaciones/Leer",new() { ["id"]=notification.Id.ToString() });
            Assert.Equal(HttpStatusCode.Redirect,saved.StatusCode);
            Assert.True(await audit.App.Db(db=>db.Notificaciones.Where(n=>n.Id==notification.Id).Select(n=>n.Leida).SingleAsync()));
        }
    }

    [Theory]
    [InlineData("Id")][InlineData("Version")][InlineData("Habilitado")]
    public async Task Partial_permission_rows_cannot_silently_disable_a_capability(string omitted)
    {
        var fields=new Dictionary<string,string> { ["Rol"]="Coordinador" };
        foreach(var (p,n) in Permisos.Catalogo.Where(p=>p.Rol==RolUsuario.Coordinador).Select((p,n)=>(p,n)))
        { fields[$"Permisos[{n}].Clave"]=p.Clave;fields[$"Permisos[{n}].Version"]="0";fields[$"Permisos[{n}].Habilitado"]="true"; }
        fields.Remove("Permisos[0]."+(omitted=="Id"?"Clave":omitted));
        await audit.Client("administrador");var before=await Fingerprint();
        using var response=await audit.Post("administrador","/Administracion/EditarPermisos",fields);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(before,await Fingerprint());
    }
}

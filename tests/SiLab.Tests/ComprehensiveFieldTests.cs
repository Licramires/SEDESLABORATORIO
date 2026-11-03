using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class ComprehensiveAuditTests
{
    public static IEnumerable<object[]> InvalidFields()
    {
        object[] C(string form,string field,string value)=>[form,field,value];
        foreach(var (form,field,max) in new[] {
            ("nueva","NombreLaboratorio",200),("nueva","TipoLaboratorio",100),("nueva","Municipio",100),
            ("usuario","Nombre",120),("usuario","Email",200),("requisito","Nombre",200),
            ("seccion","Nombre",200),("catalogo","Nombre",200),("edicion","NombreLaboratorio",200),
            ("edicion","TipoLaboratorio",100),("edicion","Municipio",100) })
        {
            foreach(var value in new[]{"<omitir>","","   ",new string('a',max+1),"Texto\0oculto"})yield return C(form,field,value);
        }
        foreach(var form in new[]{"nueva","edicion"})
        {
            foreach(var field in new[]{"Latitud","Longitud"})foreach(var value in new[]{"<omitir>","","abc","NaN","Infinity","1,234.56","1e2","9999999999999999999999999999999999999999999"})yield return C(form,field,value);
            yield return C(form,"Latitud","90.000001");yield return C(form,"Latitud","-90.000001");
            yield return C(form,"Longitud","180.000001");yield return C(form,"Longitud","-180.000001");
            yield return C(form,"Direccion",new string('a',501));yield return C(form,"Codigo",new string('a',101));
            foreach(var value in new[]{"abcdef","hola@correo","555\0oculto",new string('1',51)})yield return C(form,"Telefono",value);
        }
        foreach(var value in new[]{"sin-arroba","x@@y.test","x@","@y.test","a b@example.test","x..y@example.test",".x@example.test","x@example..test","x@-example.test"})yield return C("usuario","Email",value);
        foreach(var value in new[]{"123456789",new string('x',129),"<omitir>",""})yield return C("usuario","Password",value);
        foreach(var value in new[]{"999","-1","administrador-inventado","<omitir>"})yield return C("usuario","Rol",value);
        foreach(var form in new[]{"usuario","requisito","catalogo"})foreach(var value in new[]{"no-es-bool","<omitir>"})yield return C(form,"Activo",value);
        foreach(var value in new[]{"no-es-bool","<omitir>"})yield return C("requisito","Obligatorio",value);
        foreach(var value in new[]{"no-es-bool","<omitir>"})yield return C("seccion","Activa",value);
        foreach(var form in new[]{"requisito","seccion"})foreach(var value in new[]{"-1","10000","número","2147483648"})yield return C(form,"Orden",value);
        foreach(var value in new[]{"-1","999999","texto"})yield return C("requisito","SeccionId",value);
        yield return C("seccion","Descripcion",new string('a',1001));yield return C("seccion","Descripcion","Texto\0oculto");
        foreach(var value in new[]{"inventado","<omitir>",""})yield return C("catalogo","Clase",value);
        yield return C("catalogo","Referencia",new string('a',1001));yield return C("catalogo","Referencia","Texto\0oculto");
        foreach(var form in new[]{"acta","revision","citacion","edicion"})
            foreach(var field in new[]{"Id","Version"})foreach(var value in new[]{"<omitir>","no-numérico","2147483648","-1"})yield return C(form,field,value);
        foreach(var value in new[]{"<omitir>","no-numérico","-1","999999"})yield return C("revision","DocumentoId",value);
        foreach(var value in new[]{"inventado","<omitir>",""})yield return C("revision","Resultado",value);
        foreach(var value in new[]{"<omitir>","",new string('a',2001),"Texto\0oculto"})yield return C("revision","Motivo",value);
        foreach(var value in new[]{new string('a',4001),"Texto\0oculto"})yield return C("acta","Motivo",value);
        foreach(var value in new[]{"inventado",new string('a',31)})yield return C("acta","Resultado",value);
        foreach(var value in new[]{"0","91","no-numérico","2147483648"})yield return C("acta","DiasSubsanacion",value);
        foreach(var value in new[]{"<omitir>","",new string('a',2001),"Texto\0oculto"})yield return C("citacion","Motivo",value);
        foreach(var field in new[]{"FechaEmision","Vencimiento"})foreach(var value in new[]{"<omitir>","","no-fecha","2026-02-30","2026-10-22T09:00","2026-10-22T00:00Z"})yield return C("citacion",field,value);
        foreach(var value in new[]{"-1","31","no-numérico"})yield return C("citacion","AnticipacionDias",value);
        foreach(var value in new[]{"<omitir>","no-numérico","-1","999999"})yield return C("citacion","InspeccionId",value);
        foreach(var value in new[]{new string('a',2001),"Texto\0oculto"})yield return C("servicios","servicios",value);
    }

    private async Task<(string Role,string Route,Dictionary<string,string> Fields,byte[]? File,string FileField)> Form(string name)
    {
        var fields=new Dictionary<string,string>(); var role="administrador";var route="/Administracion/";byte[]? file=null;var fileField="Pdf";
        switch(name)
        {
            case "nueva":case "edicion":
                role="propietario";route="/solicitud-registro/SolicitudRegistro/Nueva";
                fields=new() { ["NombreLaboratorio"]="Laboratorio de control",["TipoLaboratorio"]="Clínico",["Municipio"]="Cochabamba",["Latitud"]="-17.39",["Longitud"]="-66.15" };
                if(name=="edicion"){var s=await audit.Case(EstadoSolicitud.Borrador);fields["Id"]=s.Id.ToString();fields["Version"]=s.Version.ToString();route="/Gestion/EditarDatos";}
                break;
            case "usuario":route+="Usuario";fields=new() { ["Nombre"]="Usuario de control",["Email"]=Guid.NewGuid()+"@example.test",["Password"]="PruebaSegura123!",["Rol"]="Propietario",["Activo"]="true" };break;
            case "requisito":route+="Requisito";fields=new() { ["Nombre"]="Requisito "+Guid.NewGuid(),["Obligatorio"]="false",["Activo"]="true",["Orden"]="0" };break;
            case "seccion":route+="Seccion";fields=new() { ["Nombre"]="Sección "+Guid.NewGuid(),["Activa"]="true",["Orden"]="0" };break;
            case "catalogo":route+="Catalogo";fields=new() { ["Clase"]="tipo",["Nombre"]="Tipo "+Guid.NewGuid(),["Activo"]="true" };break;
            case "servicios":
                role="propietario";route="/Gestion/Servicios";
                var lab=await audit.App.Db(db=>db.Laboratorios.SingleAsync(l=>l.Codigo=="DEMO-LAB-HABILITADO"));
                fields=new() { ["id"]=lab.Id.ToString(),["version"]=RevisionServicios.De(lab.Servicios),["servicios"]="Hematología" };break;
            case "acta":
                role="supervisor";route="/Gestion/Decidir";file=Pdf;var acta=await audit.Case(EstadoSolicitud.Inspeccion);fields=Fields(acta,"acta");
                var checks=ExpedienteService.LeerVerificaciones(acta.Inspecciones.Single());
                for(var n=0;n<checks.Count;n++){fields[$"Verificaciones[{n}].Id"]=checks[n].Id.ToString();fields[$"Verificaciones[{n}].Resultado"]="cumple";}
                break;
            case "revision":
                role="coordinador";route="/Gestion/RevisarDocumento";var doc=await audit.Case(EstadoSolicitud.EnRevision);
                fields=new() { ["Id"]=doc.Id.ToString(),["Version"]=doc.Version.ToString(),["DocumentoId"]=doc.Documentos.First().Id.ToString(),["Resultado"]="observado",["Motivo"]="Corregir el documento" };break;
            case "citacion":
                role="supervisor";route="/Gestion/Citacion";file=Pdf;fileField="Evidencia";var citation=await audit.Case(EstadoSolicitud.Observado);
                fields=new() { ["Id"]=citation.Id.ToString(),["Version"]=citation.Version.ToString(),["InspeccionId"]=citation.Inspecciones.Single().Id.ToString(),["Motivo"]="Corregir los incumplimientos",["FechaEmision"]=TramiteService.HoraBolivia.ToString("yyyy-MM-dd"),["Vencimiento"]=TramiteService.HoraBolivia.AddDays(5).ToString("yyyy-MM-dd"),["AnticipacionDias"]="5" };break;
            default:throw new InvalidOperationException(name);
        }
        return(role,route,fields,file,fileField);
    }

    private Task<string> Fingerprint()=>audit.App.Db(async db=>{
        var connection=db.Database.GetDbConnection();await connection.OpenAsync();var names=new List<string>();
        await using(var command=connection.CreateCommand()){command.CommandText="select name from sqlite_master where type='table' and name not like 'sqlite_%' order by name";await using var reader=await command.ExecuteReaderAsync();while(await reader.ReadAsync())names.Add(reader.GetString(0));}
        var data=new Dictionary<string,List<object?[]>>();
        foreach(var name in names){await using var command=connection.CreateCommand();command.CommandText="select * from \""+name+"\" order by rowid";await using var reader=await command.ExecuteReaderAsync();var rows=new List<object?[]>();while(await reader.ReadAsync()){var values=new object[reader.FieldCount];reader.GetValues(values);rows.Add(values.Select(v=>v==DBNull.Value?null:v).ToArray());}data[name]=rows;}
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data))));
    });

    [Theory,MemberData(nameof(InvalidFields))]
    public async Task Every_form_field_rejects_invalid_values_without_changing_any_table(string form,string field,string value)
    {
        var payload=await Form(form);await audit.Client(payload.Role);
        if(value=="<omitir>")payload.Fields.Remove(field);else payload.Fields[field]=value;
        var before=await Fingerprint();
        using var response=await audit.Post(payload.Role,payload.Route,payload.Fields,payload.File,payload.FileField);
        Assert.True((int)response.StatusCode<500,$"{form}.{field}: {(int)response.StatusCode}");
        Assert.True(before==await Fingerprint(),$"Se guardó un valor inválido: {form}.{field} = {value.Replace("\0","[NUL]")}");
    }
}

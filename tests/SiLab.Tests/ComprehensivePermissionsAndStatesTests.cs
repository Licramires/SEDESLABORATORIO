using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class ComprehensiveAuditTests
{
    [Theory]
    [InlineData("0001-01-01")][InlineData("9999-12-31")]
    public async Task Agenda_and_routes_accept_civil_calendar_boundaries_without_server_overflow(string day)
    {
        var(client,_)=await audit.Client("supervisor");
        foreach(var action in new[]{"Agenda","Rutas"})
        { using var response=await client.GetAsync($"/Gestion/{action}?fecha={day}");Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains(day,await response.Content.ReadAsStringAsync()); }
    }

    [Fact]
    public async Task Every_configurable_capability_is_enforced_on_the_existing_session()
    {
        var result=new List<object>();
        foreach(var cap in Permisos.Catalogo)
        {
            var role=cap.Rol.ToString().ToLowerInvariant();await audit.Client(role);
            var s=await audit.Case(cap.Clave switch { "documentos"=>EstadoSolicitud.EnRevision,"asignaciones"=>EstadoSolicitud.Aprobado,"actas"=>EstadoSolicitud.RevisionActa,"legal"=>EstadoSolicitud.RevisionLegal,_=>EstadoSolicitud.Borrador });
            var route=cap.Clave switch { "solicitudes"=>"/solicitud-registro/SolicitudRegistro/Nueva","establecimientos"=>"/Gestion/Establecimientos","informes"=>$"/Gestion/Informe/{s.Id}","inspecciones"=>"/Gestion/Agenda","metricas"=>"/Metricas","usuarios"=>"/Administracion/Usuarios","requisitos"=>"/Administracion/Requisitos",_=>"/Gestion/Decidir" };
            var fields=Fields(s,cap.Clave switch { "documentos"=>"aprobar_documentos","asignaciones"=>"asignar","actas"=>"aprobar_acta",_=>"aprobar_legal" });
            var setting=new Dictionary<string,string> { ["rol"]=cap.Rol.ToString(),["clave"]=cap.Clave,["habilitado"]="false" };
            using var disabled=await audit.Post("administrador","/Administracion/Permiso",setting);
            Assert.Equal(HttpStatusCode.Redirect,disabled.StatusCode);
            try
            {
                var before=await Fingerprint();var (client,_)=await audit.Client(role);
                using var response=route=="/Gestion/Decidir"?await audit.Post(role,route,fields,Pdf):await client.GetAsync(route);
                Assert.Contains("AccessDenied",response.Headers.Location?.ToString()??"");
                Assert.Equal(before,await Fingerprint());
                result.Add(new { Role=role,Capability=cap.Clave,Route=route,Denied=true });
            }
            finally { setting["habilitado"]="true";using var restored=await audit.Post("administrador","/Administracion/Permiso",setting);Assert.Equal(HttpStatusCode.Redirect,restored.StatusCode); }
        }
        Assert.Equal(11,result.Count);ExhaustiveFixture.Report("permisos-configurables",result);
    }

    [Fact]
    public async Task Detail_buttons_and_forms_are_inventoried_for_every_role_and_workflow_state()
    {
        var report=new List<object>();
        foreach(var state in Enum.GetValues<EstadoSolicitud>())
        {
            var s=await audit.Case(state);
            foreach(var role in Roles)
            {
                var (client,_)=await audit.Client(role);using var response=await client.GetAsync($"/Gestion/Detalle/{s.Id}");
                Assert.True((int)response.StatusCode<500,$"Detalle {role}/{state}: {(int)response.StatusCode}");
                var html=await response.Content.ReadAsStringAsync();
                var actions=Regex.Matches(html,@"<(?:input|button)\b[^>]*name=""Accion""[^>]*value=""([^""]+)""",RegexOptions.IgnoreCase).Select(m=>WebUtility.HtmlDecode(m.Groups[1].Value)).Distinct().ToArray();
                foreach(var action in actions)
                {
                    var transition=Transitions.Single(t=>t.Action==action);
                    Assert.True(role==transition.Role&&transition.From.Contains(state),$"Botón que no corresponde al estado: {role}/{state}/{action}");
                }
                var buttons=Regex.Matches(html,@"<button\b[^>]*>[\s\S]*?</button>",RegexOptions.IgnoreCase).Select(m=>WebUtility.HtmlDecode(Regex.Replace(m.Value,"<[^>]+>","")).Trim()).ToArray();
                var fields=Regex.Matches(html,@"<(?:input|textarea|select)\b[^>]*name=""([^""]+)""",RegexOptions.IgnoreCase).Select(m=>m.Groups[1].Value).Where(n=>n!="__RequestVerificationToken").Distinct().ToArray();
                report.Add(new { Role=role,State=state.ToString(),Status=(int)response.StatusCode,Actions=actions,Buttons=buttons,Fields=fields });
            }
        }
        ExhaustiveFixture.Report("botones-por-rol-y-estado",report);
    }

    [Theory]
    [InlineData("/Gestion?estado=no-existe","coordinador")]
    [InlineData("/Gestion?estado=999","propietario")]
    [InlineData("/Gestion/Agenda?fecha=no-fecha","supervisor")]
    [InlineData("/Gestion/Rutas?fecha=2026-02-30","supervisor")]
    [InlineData("/publico/Publico?estado=no-existe","anonimo")]
    [InlineData("/publico/Publico?estado=999","anonimo")]
    [InlineData("/Administracion/Usuario?id=no-numero","administrador")]
    [InlineData("/Administracion/Requisito?id=no-numero","administrador")]
    [InlineData("/Administracion/Seccion?id=no-numero","administrador")]
    [InlineData("/Administracion/Catalogo?id=no-numero","administrador")]
    [InlineData("/Administracion/EditarPermisos?rol=999","administrador")]
    [InlineData("/Metricas/Datos?desde=2026-02-30","gerente")]
    public async Task Invalid_query_fields_are_rejected_without_opening_an_unfiltered_or_creation_screen(string route,string role)
    {
        var(client,_)=await audit.Client(role);using var response=await client.GetAsync(route);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
    }
}

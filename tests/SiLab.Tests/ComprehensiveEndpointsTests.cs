using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class ComprehensiveAuditTests
{
    private IEnumerable<ControllerActionDescriptor> Endpoints => audit.App.Services
        .GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>();
    private static string[] Methods(ControllerActionDescriptor a) => a.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(c=>c.HttpMethods).Distinct().ToArray() is { Length:>0 } methods ? methods : ["GET"];
    private static string Route(ControllerActionDescriptor a) => "/" + (a.RouteValues.TryGetValue("area",out var area) && !string.IsNullOrEmpty(area) ? area+"/" : "") + a.ControllerName + "/" + a.ActionName;
    private static bool Allowed(ControllerActionDescriptor a,string role)
    {
        var attributes=a.ControllerTypeInfo.GetCustomAttributes<AuthorizeAttribute>().Concat(a.MethodInfo.GetCustomAttributes<AuthorizeAttribute>());
        return attributes.All(x=>role!="anonimo"&&(string.IsNullOrEmpty(x.Roles)||x.Roles.Split(',').Contains(role)));
    }
    private static string Attr(string tag,string name) => WebUtility.HtmlDecode(Regex.Match(tag,@"\b"+name+"=\"([^\"]*)\"",RegexOptions.IgnoreCase).Groups[1].Value);

    [Fact]
    public async Task Every_declared_page_download_and_role_is_exercised_and_rendered_links_and_forms_are_checked()
    {
        var s=await audit.Case(EstadoSolicitud.RevisionLegal);
        var draft=await audit.Case(EstadoSolicitud.Borrador);
        var closed=await audit.Case(EstadoSolicitud.Habilitado);
        var requirementName="Plantilla de auditoría "+Guid.NewGuid();
        using(var configured=await audit.Post("administrador","/Administracion/Requisito",new() { ["Nombre"]=requirementName,["Obligatorio"]="false",["Activo"]="true",["Orden"]="0" },Pdf,"Plantilla"))
            Assert.Equal(HttpStatusCode.Redirect,configured.StatusCode);
        var template=await audit.App.Db(db=>db.Catalogos.SingleOrDefaultAsync(c=>c.Clase=="formulario_citacion"&&c.Activo));
        using(var configured=await audit.Post("administrador","/Administracion/Catalogo",new() { ["Id"]=(template?.Id??0).ToString(),["Version"]=(template?.Version??0).ToString(),["Nombre"]=template?.Nombre??"Citación de auditoría",["Clase"]="formulario_citacion",["Activo"]="true" },Pdf,"Plantilla"))
            Assert.Equal(HttpStatusCode.Redirect,configured.StatusCode);
        var data=await audit.App.Db(async db=>new {
            Lab=await db.Laboratorios.Where(l=>l.Codigo=="DEMO-LAB-HABILITADO").Select(l=>l.Id).SingleAsync(),
            Requirement=await db.Publicados().Where(r=>r.PlantillaPdf!=null&&r.PlantillaPdf!="").Select(r=>r.Id).FirstAsync(),
            User=await db.Usuarios.Where(u=>u.Email=="propietario@si-lab.local").Select(u=>u.Id).SingleAsync()
        });
        var endpoints=Endpoints.ToList();
        var knownPost=endpoints.Where(a=>Methods(a).Contains("POST")).Select(Route).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var results=new List<object>();var controls=new List<object>();var linksChecked=new HashSet<string>();
        foreach(var a in endpoints.Where(a=>Methods(a).Contains("GET")))foreach(var role in Roles.Append("anonimo"))
        {
            var url=Route(a);var id=s.Id;
            if(a.ControllerName=="Publico")id=a.ActionName=="Plantilla"?data.Requirement:data.Lab;
            if(a.ControllerName=="Administracion")id=a.ActionName=="Usuario"?data.User:data.Requirement;
            if(a.ControllerName=="Gestion")id=a.ActionName switch { "Descargar"=>s.Documentos.First().Id,"EditarDatos"=>draft.Id,"Informe"=>closed.Id,"Establecimiento"=>data.Lab,_=>s.Id };
            if(a.ActionName is "Seccion" or "Catalogo")url+="?rol=Propietario";
            else url+=$"?id={id}&laboratorioId={data.Lab}&rol=Propietario";
            var (client,_)=await audit.Client(role);
            using var response=await client.GetAsync(url);
            var allowed=Allowed(a,role);var status=(int)response.StatusCode;
            if(!allowed)Assert.True(status is 302 or 401 or 403,$"Acceso indebido {role} {url}: {status}");
            else Assert.True(status is 200 or 302,$"Página {role} {url}: {status}: {await response.Content.ReadAsStringAsync()}");
            results.Add(new { Role=role,Action=a.ControllerName+"."+a.ActionName,Url=url,Allowed=allowed,Status=status });
            if(response.Content.Headers.ContentType?.MediaType=="application/pdf")
                Assert.StartsWith("%PDF-",System.Text.Encoding.ASCII.GetString(await response.Content.ReadAsByteArrayAsync()));
            if(status!=200 || response.Content.Headers.ContentType?.MediaType!="text/html")continue;
            var html=await response.Content.ReadAsStringAsync();
            foreach(Match form in Regex.Matches(html,@"<form\b[^>]*>[\s\S]*?</form>",RegexOptions.IgnoreCase))
            {
                var action=Attr(form.Value,"action");var method=Attr(form.Value,"method").ToUpperInvariant();
                if(string.IsNullOrEmpty(action))action=Route(a);
                if(method=="POST")
                {
                    Assert.Contains("name=\"__RequestVerificationToken\"",form.Value);
                    Assert.True(knownPost.Contains(action.Split('?')[0]),$"Formulario sin destino POST declarado: {role} {url} -> {action}");
                }
                var fields=Regex.Matches(form.Value,@"<(?:input|textarea|select)\b[^>]*>",RegexOptions.IgnoreCase).Select(m=>Attr(m.Value,"name")).Where(n=>n.Length>0&&n!="__RequestVerificationToken").Distinct().ToArray();
                var buttons=Regex.Matches(form.Value,@"<button\b[^>]*>[\s\S]*?</button>",RegexOptions.IgnoreCase).Select(m=>new { Name=Attr(m.Value,"name"),Value=Attr(m.Value,"value"),Type=Attr(m.Value,"type"),Text=Regex.Replace(m.Value,"<[^>]+>","").Trim() }).ToArray();
                Assert.All(buttons,b=>Assert.True(b.Text.Length>0,$"Botón sin texto en {url}"));
                controls.Add(new { Role=role,Page=url,Action=action,Method=method,Fields=fields,Buttons=buttons });
            }
            foreach(Match link in Regex.Matches(html,@"<a\b[^>]*href=""([^""]+)""",RegexOptions.IgnoreCase))
            {
                var href=WebUtility.HtmlDecode(link.Groups[1].Value);
                if(!href.StartsWith('/')||href.StartsWith("//")||!linksChecked.Add(role+":"+href))continue;
                using var target=await client.GetAsync(href);
                Assert.True((int)target.StatusCode<400,$"Enlace roto: {role} {url} -> {href}: {(int)target.StatusCode}");
            }
        }
        ExhaustiveFixture.Report("paginas-descargas-por-rol",results);
        ExhaustiveFixture.Report("formularios-botones-renderizados",controls);
        ExhaustiveFixture.Report("enlaces-verificados",linksChecked.Order().ToArray());
        ExhaustiveFixture.Report("inventario-endpoints",endpoints.Select(a=>new { a.ControllerName,a.ActionName,Methods=Methods(a),Url=Route(a) }));
    }

    [Fact]
    public async Task Every_post_endpoint_rejects_missing_csrf_and_unauthorized_roles_without_any_persistent_effect()
    {
        var report=new List<object>();
        foreach(var a in Endpoints.Where(a=>Methods(a).Contains("POST")))foreach(var role in Roles.Append("anonimo"))
        {
            await audit.Client(role);var before=await Fingerprint();
            using(var missing=await audit.Post(role,Route(a),[],csrf:false))
            {
                Assert.True((int)missing.StatusCode is 400 or 302 or 401 or 403,$"Falta protección CSRF: {role} {Route(a)} {(int)missing.StatusCode}");
                Assert.Equal(before,await Fingerprint());
                report.Add(new { Role=role,Url=Route(a),Test="Sin CSRF",Status=(int)missing.StatusCode });
            }
            if(Allowed(a,role))continue;
            using var forbidden=await audit.Post(role,Route(a),[]);
            Assert.True((int)forbidden.StatusCode is 302 or 401 or 403,$"Permiso incorrecto: {role} {Route(a)} {(int)forbidden.StatusCode}");
            Assert.Equal(before,await Fingerprint());
            report.Add(new { Role=role,Url=Route(a),Test="Otro rol",Status=(int)forbidden.StatusCode });
        }
        ExhaustiveFixture.Report("seguridad-todos-post",report);
    }
}

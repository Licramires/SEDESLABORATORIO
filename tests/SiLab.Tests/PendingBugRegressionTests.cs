using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public partial class ComprehensiveAuditTests
{
    private async Task<HttpResponseMessage> PostHtml(string role, string path, Dictionary<string,string> fields)
    {
        var (client,token)=await audit.Client(role);
        using var request=new HttpRequestMessage(HttpMethod.Post,path);
        request.Headers.Accept.ParseAdd("text/html");
        request.Content=new FormUrlEncodedContent(new Dictionary<string,string>(fields){["__RequestVerificationToken"]=token});
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Stale_browser_post_keeps_409_and_renders_a_get_link_to_the_current_case()
    {
        var s=await audit.Case(EstadoSolicitud.Borrador);
        var fields=new Dictionary<string,string>{["Id"]=s.Id.ToString(),["Version"]="1",["Accion"]="enviar"};
        Assert.Equal(HttpStatusCode.Redirect,(await audit.Post("propietario","/Gestion/Decidir",fields)).StatusCode);
        using var response=await PostHtml("propietario","/Gestion/Decidir",fields);
        Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
        Assert.Equal("text/html",response.Content.Headers.ContentType?.MediaType);
        var page=WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Ver versión actual del trámite",page);Assert.Contains("href=\"/Gestion/Detalle/"+s.Id+"\"",page);
        Assert.Contains("Menú de la aplicación",page);Assert.Contains("Ir a mi cuenta",page);
        var (owner,_)=await audit.Client("propietario");
        Assert.Equal(HttpStatusCode.OK,(await owner.GetAsync("/Gestion/Detalle/"+s.Id)).StatusCode);
        await audit.App.Db(async db=>{Assert.Equal(2,(await db.Solicitudes.FindAsync(s.Id))!.Version);Assert.Single(await db.HistorialTramites.Where(h=>h.SolicitudId==s.Id).ToListAsync());return 0;});
    }

    [Fact]
    public async Task Browser_validation_error_has_recovery_but_http_clients_keep_plain_validation()
    {
        var s=await audit.Case(EstadoSolicitud.EnRevision);var doc=s.Documentos.First();
        var fields=new Dictionary<string,string>{["Id"]=s.Id.ToString(),["Version"]="1",["DocumentoId"]=doc.Id.ToString(),["Resultado"]="observado",["Motivo"]=""};
        var before=await Fingerprint();
        using var browser=await PostHtml("coordinador","/Gestion/RevisarDocumento",fields);
        Assert.Equal(HttpStatusCode.BadRequest,browser.StatusCode);
        var page=WebUtility.HtmlDecode(await browser.Content.ReadAsStringAsync());
        Assert.Contains("Indique qué debe corregirse",page);Assert.Contains("href=\"/Gestion/Detalle/"+s.Id+"\"",page);
        Assert.Equal("text/html",browser.Content.Headers.ContentType?.MediaType);
        using var http=await audit.Post("coordinador","/Gestion/RevisarDocumento",fields);
        Assert.Equal(HttpStatusCode.BadRequest,http.StatusCode);Assert.Equal("text/plain",http.Content.Headers.ContentType?.MediaType);
        Assert.Equal(before,await Fingerprint());
    }

    [Fact]
    public async Task Reapproving_a_current_document_is_idempotent_and_observation_remains_available()
    {
        var s=await audit.Case(EstadoSolicitud.EnRevision);var document=s.Documentos.First();
        await audit.App.Db(async db=>{(await db.Documentos.FindAsync(document.Id))!.EstadoRevision="pendiente";return await db.SaveChangesAsync();});
        var fields=new Dictionary<string,string>{["Id"]=s.Id.ToString(),["Version"]="1",["DocumentoId"]=document.Id.ToString(),["Resultado"]="aprobado",["Motivo"]=""};
        Assert.Equal(HttpStatusCode.Redirect,(await audit.Post("coordinador","/Gestion/RevisarDocumento",fields)).StatusCode);
        var before=await Fingerprint();fields["Version"]="2";
        Assert.Equal(HttpStatusCode.Redirect,(await audit.Post("coordinador","/Gestion/RevisarDocumento",fields)).StatusCode);
        Assert.Equal(before,await Fingerprint());
        var (coordinator,_)=await audit.Client("coordinador");var page=await coordinator.GetStringAsync("/Gestion/Detalle/"+s.Id);
        Assert.DoesNotContain(">Aprobar este documento</button>",page);Assert.Contains(">Observar este documento</button>",page);
        fields["Version"]="1";
        Assert.Equal(HttpStatusCode.Conflict,(await audit.Post("coordinador","/Gestion/RevisarDocumento",fields)).StatusCode);
        Assert.Equal(before,await Fingerprint());
        fields["Version"]="2";fields["Resultado"]="observado";fields["Motivo"]="Corregir la licencia";
        Assert.Equal(HttpStatusCode.Redirect,(await audit.Post("coordinador","/Gestion/RevisarDocumento",fields)).StatusCode);
        await audit.App.Db(async db=>{var d=await db.Documentos.FindAsync(document.Id);Assert.Equal("observado",d!.EstadoRevision);Assert.Equal(3,(await db.Solicitudes.FindAsync(s.Id))!.Version);Assert.Equal(2,await db.HistorialTramites.CountAsync(h=>h.SolicitudId==s.Id));return 0;});
    }

    [Theory]
    [InlineData(91,0)] [InlineData(-91,0)] [InlineData(0,181)] [InlineData(0,-181)] [InlineData(999,999)]
    public async Task Invalid_persisted_coordinates_remain_consultable_without_markers_or_maps_destinations(int lat,int lng)
    {
        var name="Ubicación dañada "+Guid.NewGuid().ToString("N");
        var id=await audit.App.Db(async db=>{var owner=await db.Usuarios.FirstAsync(u=>u.Rol==RolUsuario.Propietario);var lab=new Laboratorio{Nombre=name,Tipo="Clínico",Lat=lat,Lng=lng,Estado=EstadoLaboratorio.Abierto,DatosVerificados=true,PropietarioId=owner.Id,VigenciaHasta=DateTime.UtcNow.AddMonths(1),Direccion="Dirección consultable"};db.Laboratorios.Add(lab);await db.SaveChangesAsync();return lab.Id;});
        using var visitor=audit.App.Browser();
        var listing=WebUtility.HtmlDecode(await visitor.GetStringAsync("/publico/Publico"));
        Assert.Contains(name,listing);var encoded=Regex.Match(listing,"data-laboratories=\"(.*?)\" aria-label").Groups[1].Value;
        // La comprobación se hace sobre el atributo original para no romper el
        // delimitador HTML al decodificar las comillas internas del JSON.
        var original=await visitor.GetStringAsync("/publico/Publico");
        encoded=WebUtility.HtmlDecode(Regex.Match(original,"data-laboratories=\"([^\"]*)\"").Groups[1].Value);
        using(var json=JsonDocument.Parse(encoded))Assert.DoesNotContain(json.RootElement.EnumerateArray(),lab=>lab.GetProperty("Id").GetInt32()==id);
        var detail=WebUtility.HtmlDecode(await visitor.GetStringAsync("/publico/Publico/Detalle/"+id));
        Assert.Contains("pendiente de corrección",detail);Assert.Contains("Dirección consultable",detail);Assert.DoesNotContain("id=\"detail-map\"",detail);
        var route=await visitor.GetStringAsync("/publico/Publico/Ruta?laboratorioId="+id);
        Assert.DoesNotContain("id=\"route-link\"",route);
        Assert.DoesNotContain("href=\"https://www.google.com/maps",route);
        Assert.DoesNotContain("destination="+lat+","+lng,route);
        Assert.Contains("pendiente de correcci",WebUtility.HtmlDecode(route));
    }

    [Theory]
    [InlineData(-90,-180)] [InlineData(90,180)] [InlineData(0,0)] [InlineData(-17,-66)]
    public async Task Valid_coordinate_boundaries_still_offer_routes(int lat,int lng)
    {
        Assert.True(Coordenadas.Validar(lat,lng));
        var id=await audit.App.Db(async db=>{var lab=new Laboratorio{Nombre="Ubicación límite",Tipo="Clínico",Lat=lat,Lng=lng,Estado=EstadoLaboratorio.Abierto};db.Laboratorios.Add(lab);await db.SaveChangesAsync();return lab.Id;});
        using var visitor=audit.App.Browser();var route=WebUtility.HtmlDecode(await visitor.GetStringAsync("/publico/Publico/Ruta?laboratorioId="+id));
        var destination=Regex.Match(route,"[?&]destination=([^\"&]+)");Assert.True(destination.Success);
        var coordinates=Uri.UnescapeDataString(destination.Groups[1].Value).Split(',');Assert.Equal(2,coordinates.Length);
        Assert.Equal((decimal)lat,decimal.Parse(coordinates[0],System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal((decimal)lng,decimal.Parse(coordinates[1],System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("id=\"route-link\"",route);
    }
}

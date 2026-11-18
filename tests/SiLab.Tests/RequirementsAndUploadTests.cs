using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using Xunit;

namespace SiLab.Tests;

public partial class WorkflowTests
{
    [Fact]
    public async Task Requirements_have_a_public_menu_page_and_are_visible_during_registration()
    {
        using var anonymous = app.Browser(); using var owner = await Login("propietario");
        var hiddenId = await app.Db(async db => {
            var r = new Requisito { Nombre = "Requisito retirado invisible de prueba", Activo = false };
            db.Requisitos.Add(r); await db.SaveChangesAsync(); return r.Id;
        });
        var page = WebUtility.HtmlDecode(await anonymous.GetStringAsync("/laboratorios/Requerimientos"));
        Assert.Contains("Requerimientos para registrar un laboratorio", page);
        Assert.DoesNotContain("Requisito retirado invisible de prueba", page);
        Assert.Contains("Obligatorio para enviar", page);
        var form = WebUtility.HtmlDecode(await owner.GetStringAsync("/solicitud-registro/SolicitudRegistro/Nueva"));
        var active = await app.Db(db => db.Requisitos.Where(r => r.Activo).Select(r => r.Nombre).ToListAsync());
        foreach (var name in active) { Assert.Contains(name, page); Assert.Contains(name, form); }
        Assert.Matches("href=\"[^\"]*/Requerimientos\"", form);
        Assert.Contains("pdf-selection-list", form);
        Assert.DoesNotContain("hasta 5 PDF", form);
        var home = await anonymous.GetStringAsync("/laboratorios");
        Assert.Matches("href=\"[^\"]*/Requerimientos\"", home);
        await app.Db(async db => { db.Requisitos.Remove((await db.Requisitos.FindAsync(hiddenId))!); return await db.SaveChangesAsync(); });
    }

    [Fact]
    public async Task More_than_five_PDFs_can_be_added_then_removed_individually_before_submitting()
    {
        using var owner = await Login("propietario");
        var csrf = await Token(owner, "/solicitud-registro/SolicitudRegistro/Nueva");
        using var form = new MultipartFormDataContent();
        foreach (var pair in new Dictionary<string,string> { ["NombreLaboratorio"] = "Carga acumulada de ocho PDF",
            ["TipoLaboratorio"] = "Clínico", ["Municipio"] = "Sacaba", ["Latitud"] = "-17.39", ["Longitud"] = "-66.15", ["__RequestVerificationToken"] = csrf })
            form.Add(new StringContent(pair.Value), pair.Key);
        for (var n = 0; n < 8; n++) form.Add(new ByteArrayContent(Pdf), "DocumentosPdf", $"archivo-{n}.pdf");
        var created = await owner.PostAsync("/solicitud-registro/SolicitudRegistro/Nueva", form);
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var id = int.Parse(Regex.Match(created.Headers.Location!.ToString(), @"\d+$").Value);
        Assert.Equal(8, await app.Db(db => db.Documentos.CountAsync(d => d.SolicitudId == id && d.Actual)));
        var filenames = await app.Db(db => db.Documentos.Where(d => d.SolicitudId == id).Select(d => d.NombreArchivo).ToListAsync());
        Assert.Equal(Enumerable.Range(0, 8).Select(n => $"archivo-{n}.pdf").Order(), filenames.Order());
        await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/Documento", new() { ["id"] = id.ToString(),
            ["version"] = (await Version(id)).ToString(), ["requisitoId"] = "0" }, Pdf, "pdf");
        Assert.Equal(9, await app.Db(db => db.Documentos.CountAsync(d => d.SolicitudId == id && d.Actual)));
        await UploadRequirements(owner, id);
        var doc = await app.Db(db => db.Documentos.FirstAsync(d => d.SolicitudId == id && d.RequisitoId != 0));
        var oldVersion = await Version(id);
        var removed = await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/RetirarDocumento", new() {
            ["id"] = id.ToString(), ["documentoId"] = doc.Id.ToString(), ["version"] = oldVersion.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.False(await app.Db(db => db.Documentos.Where(d => d.Id == doc.Id).Select(d => d.Actual).SingleAsync()));
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/Gestion/Descargar/{doc.Id}")).StatusCode);
        await Decide(owner, id, "enviar"); Assert.Equal(EstadoSolicitud.Borrador, await State(id));
        var stale = await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/RetirarDocumento", new() {
            ["id"] = id.ToString(), ["documentoId"] = doc.Id.ToString(), ["version"] = oldVersion.ToString() });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await Post(owner, $"/Gestion/Detalle/{id}", "/Gestion/Documento", new() { ["id"] = id.ToString(),
            ["version"] = (await Version(id)).ToString(), ["requisitoId"] = doc.RequisitoId.ToString() }, Pdf, "pdf");
        await Decide(owner, id, "enviar"); Assert.Equal(EstadoSolicitud.EnRevision, await State(id));
        var current = await app.Db(db => db.Documentos.FirstAsync(d => d.SolicitudId == id && d.Actual));
        var locked = await Post(owner, "/auth/Auth/Dashboard", "/Gestion/RetirarDocumento", new() {
            ["id"] = id.ToString(), ["documentoId"] = current.Id.ToString(), ["version"] = (await Version(id)).ToString() });
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
    }
}

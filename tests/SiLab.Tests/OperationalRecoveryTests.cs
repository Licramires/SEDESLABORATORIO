using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SEDESLABORATORIO.Infrastructure;
using Xunit;

namespace SiLab.Tests;

public sealed class OperationalRecoveryTests : IDisposable
{
    private readonly TestApplication app = new() { SeedDemoData = true };
    private static readonly byte[] Pdf = SimplePdf.Create(["Archivo sintético de recuperación"]);
    public void Dispose() => app.Dispose();

    [Fact]
    public void Another_process_cannot_start_recovery_against_a_database_in_use_and_the_lease_releases_on_disposal()
    {
        var path=Path.Combine(Path.GetTempPath(),"si-lab-lease-"+Guid.NewGuid().ToString("N")+".db");
        var configuration=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:DefaultConnection"]="Data Source="+path }).Build();
        using(var first=new LocalDatabaseLease(configuration))Assert.Throws<InvalidOperationException>(()=>new LocalDatabaseLease(configuration));
        using(var afterDisposal=new LocalDatabaseLease(configuration)) { }
        File.Delete(path+".instance.lock");
    }

    [Theory]
    [InlineData(".pdf")][InlineData(".png")][InlineData(".jpg")]
    public async Task Uncommitted_generated_files_are_preserved_privately_and_registered_and_frozen_files_are_untouched(string extension)
    {
        using var scope=app.Services.CreateScope();var storage=scope.ServiceProvider.GetRequiredService<PdfStorage>();
        var original=Directory.GetFiles(storage.Root).ToDictionary(p=>Path.GetFileName(p)!,p=>File.ReadAllBytes(p));
        var historic=Guid.NewGuid().ToString("N")+".pdf";
        await File.WriteAllBytesAsync(Path.Combine(storage.Root,historic),Pdf);
        await app.Db(async db=>{var s=await db.Solicitudes.FirstAsync();
            s.RequisitosJson=System.Text.Json.JsonSerializer.Serialize(new[]{new RequisitoCopia(123,"Plantilla histórica",true,1,historic)});
            await db.SaveChangesAsync();return 0;});
        var orphan=Guid.NewGuid().ToString("N")+extension;
        await File.WriteAllBytesAsync(Path.Combine(storage.Root,orphan),Pdf);
        var custom=Path.Combine(storage.Root,"archivo-administrativo.txt");await File.WriteAllTextAsync(custom,"Conservar");
        await StorageRecovery.RecoverAsync(app.Services);
        Assert.False(File.Exists(Path.Combine(storage.Root,orphan)));
        var recovered=Assert.Single(Directory.GetFiles(Path.Combine(storage.Root,"recuperacion"),orphan,SearchOption.AllDirectories));
        Assert.Equal(Pdf,await File.ReadAllBytesAsync(recovered));
        Assert.Equal(Pdf,await File.ReadAllBytesAsync(Path.Combine(storage.Root,historic)));
        foreach(var file in original)Assert.Equal(file.Value,await File.ReadAllBytesAsync(Path.Combine(storage.Root,file.Key!)));
        Assert.True(File.Exists(custom));
        await StorageRecovery.RecoverAsync(app.Services);
        Assert.Single(Directory.GetFiles(Path.Combine(storage.Root,"recuperacion"),orphan,SearchOption.AllDirectories));
        using var browser=app.Browser();
        Assert.Equal(HttpStatusCode.NotFound,(await browser.GetAsync("/documentos/recuperacion/"+orphan)).StatusCode);
    }

    [Fact]
    public async Task An_unreadable_snapshot_prevents_moving_any_potential_historical_evidence()
    {
        using var scope=app.Services.CreateScope();var storage=scope.ServiceProvider.GetRequiredService<PdfStorage>();
        await app.Db(async db=>{(await db.Solicitudes.FirstAsync()).RequisitosJson="[null]";await db.SaveChangesAsync();return 0;});
        var filename=Path.Combine(storage.Root,Guid.NewGuid().ToString("N")+".pdf");await File.WriteAllBytesAsync(filename,Pdf);
        await StorageRecovery.RecoverAsync(app.Services);
        Assert.Equal(Pdf,await File.ReadAllBytesAsync(filename));
    }

    [Fact]
    public async Task Unavailable_storage_returns_recoverable_503_without_persisting_a_new_request()
    {
        using var empty=new TestApplication();using var client=empty.Browser();
        static string Token(string text)=>WebUtility.HtmlDecode(Regex.Match(text,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var loginToken=Token(await client.GetStringAsync("/auth/Auth/Login"));
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync("/auth/Auth/Login",new FormUrlEncodedContent(new Dictionary<string,string>{["Email"]="propietario@si-lab.local",["Password"]="Demo123!",["__RequestVerificationToken"]=loginToken}))).StatusCode);
        var token=Token(await client.GetStringAsync("/solicitud-registro/SolicitudRegistro/Nueva"));
        using var scope=empty.Services.CreateScope();var storage=scope.ServiceProvider.GetRequiredService<PdfStorage>();Directory.CreateDirectory(Path.GetDirectoryName(storage.Root)!);
        await File.WriteAllTextAsync(storage.Root,"Raíz no disponible para el ensayo");
        try
        {
            using var body=new MultipartFormDataContent();
            foreach(var field in new Dictionary<string,string>{["NombreLaboratorio"]="Ensayo de almacenamiento",["TipoLaboratorio"]="Clínico",["Municipio"]="Cochabamba",["Latitud"]="-17.39",["Longitud"]="-66.15",["__RequestVerificationToken"]=token})body.Add(new StringContent(field.Value),field.Key);
            body.Add(new ByteArrayContent(Pdf),"DocumentosPdf","control.pdf");
            using var response=await client.PostAsync("/solicitud-registro/SolicitudRegistro/Nueva",body);
            Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
            Assert.Contains("almacenamiento",await response.Content.ReadAsStringAsync());
            Assert.Equal(0,await empty.Db(db=>db.Solicitudes.CountAsync()));Assert.Equal(0,await empty.Db(db=>db.Documentos.CountAsync()));
        }
        finally { File.Delete(storage.Root); }
    }
}

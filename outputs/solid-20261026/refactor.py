from pathlib import Path
import re,textwrap
root=Path(__file__).resolve().parents[2]
def read(p):return (root/p).read_text(encoding='utf-8-sig')
def write(p,s):
 path=root/p;path.parent.mkdir(parents=True,exist_ok=True);path.write_text(s,encoding='utf8')
def method(source,name):
 start=source.index(name);start=source.rfind('\n',0,start)+1;brace=source.index('{',start);depth=1;i=brace+1
 # Métodos extraídos no tienen llaves de texto sin pareja: interpolaciones
 # balanceadas se incluyen sin cambiar su contenido.
 while depth:
  depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[start:i]
original=read('Controllers/GestionController.cs')
workflow=original[original.index('            switch (model.Accion)'):original.index('            tramites.Registrar(s, Actor, detail);')]
pattern=r'^                case "([^"]+)":\n'
matches=list(re.finditer(pattern,workflow,re.M));groups=[];labels=[]
for i,m in enumerate(matches):
 labels.append(m.group(1));end=matches[i+1].start() if i+1<len(matches) else workflow.index('                default:')
 body=workflow[m.end():end]
 if body.strip():groups.append((labels,body));labels=[]
assert len(groups)==7
names=['EnviarSolicitud','RevisarDocumentacion','AsignarInspeccion','ProgramarInspeccion','RegistrarActa','ValidarActa','ResolverLegal']
imports='''using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

'''
for name,(actions,body) in zip(names,groups):
 body=re.sub(r'\n                    break;\s*$','\n',body)
 body=body.replace('detail = "Contenido del acta preparado para descargar, firmar y adjuntar."; break;', 'return "Contenido del acta preparado para descargar, firmar y adjuntar.";')
 body=body.replace('Require(', 'ContextoDecision.Exigir(').replace('Role(', 'contexto.ExigirRol(').replace('State(', 'contexto.ExigirEstado(')
 body=re.sub(r'\bActor\b','contexto.Actor.Id',body)
 body=body.replace('newFile','contexto.ArchivoNuevo').replace('DateTime.UtcNow','reloj.UtcNow').replace('TramiteService.HoraBolivia','reloj.HoraBolivia')
 body=body.replace('tramites.AvisarRol','avisos.AvisarRol').replace('tramites.PlazoHabil','calendario.PlazoHabil').replace('await HayConflicto(','await agenda.HayConflicto(')
 dependencies={'db':'IGestionDatos','storage':'IAlmacenPdf','avisos':'IAvisosRol','calendario':'ICalendarioLaboral','agenda':'IAgendaReservas','environment':'IWebHostEnvironment','reloj':'IReloj'}
 ctor=', '.join(f'{t} {v}' for v,t in dependencies.items() if re.search(r'\b'+v+r'\b',body))
 code=imports+f'public sealed class {name}({ctor}) : IAccionTramite\n{{\n'
 code+='    public IReadOnlyCollection<string> Acciones { get; } = ['+', '.join('"'+a+'"' for a in actions)+'];\n'
 code+='    public async Task<string> Ejecutar(ContextoDecision contexto)\n    {\n        var s = contexto.Solicitud;\n        var model = contexto.Modelo;\n        var reason = model.Motivo?.Trim() ?? "";\n        string detail;\n'
 # Una acción que no usa motivo no debe declarar una variable inútil.
 if not re.search(r'\breason\b',body):code=code.replace('        var reason = model.Motivo?.Trim() ?? "";\n','')
 code+=textwrap.indent(textwrap.dedent(body).strip(),'        ')+'\n        return detail;\n    }\n}\n'
 write('Application/Tramites/Acciones/'+name+'.cs',code)

# El controlador conserva solamente el enlace/validación HTTP y la presentación
# del resultado. Las rutas y el tratamiento especial del acta siguen iguales.
start=original.index('        await using var transaction',original.index('public async Task<IActionResult> Decidir'))
end=original.index('    private async Task<IActionResult> MostrarErrorActa',start)
new='''        var resultado = await decisiones.Ejecutar(new(Actor, User.FindFirstValue(ClaimTypes.Role) ?? ""), model);
        if (resultado.Estado == EstadoDecision.Inexistente) return NotFound();
        if (resultado.Estado == EstadoDecision.Conflicto) return Conflict(resultado.Mensaje);
        if (resultado.Estado == EstadoDecision.Invalida) return BadRequest(resultado.Mensaje);
        if (resultado.Estado == EstadoDecision.NoPermitida)
        {
            if (model.Accion is "acta" or "preparar_acta") return await MostrarErrorActa(model, resultado.Mensaje + " Los datos ingresados se conservan; vuelva a seleccionar el PDF.");
            TempData["Error"] = resultado.Mensaje;
        }
        else TempData["Success"] = resultado.Mensaje;
        return RedirectToAction(nameof(Detalle), new { model.Id });
    }

'''
write('Controllers/GestionController.cs','using SEDESLABORATORIO.Application.Tramites;\n'+original[:start]+new+original[end:])

# Adaptación de puertos conservando las consultas EF y sus traducciones a SQL.
replacements={
 'Controllers/GestionController.cs': [('ApplicationDbContext db, TramiteService tramites, PdfStorage storage, IWebHostEnvironment environment, IConfiguration configuration','IGestionDatos db, IRegistroTramite tramites, IAlmacenPdf storage, IWebHostEnvironment environment, IConfiguration configuration, IDecisionesTramite decisiones, IVisibilidadTramites visibilidad')],
 'Controllers/AdministracionController.cs':[('ApplicationDbContext db, PdfStorage storage','IAdministracionDatos db, IAlmacenPdf storage')],
 'Controllers/MetricasController.cs':[('ApplicationDbContext db','IMetricasDatos db')],
 'Controllers/NotificacionesController.cs':[('ApplicationDbContext db','INotificacionesDatos db')],
 'solicitud-registro/Controllers/SolicitudRegistroController.cs':[('ApplicationDbContext db, PdfStorage storage','IRegistroDatos db, IAlmacenPdf storage')],
 'publico/Controllers/PublicoController.cs':[('ApplicationDbContext','IPublicoDatos'),('PdfStorage','ILectorArchivos')],
 'auth/Services/AuthService.cs':[('ApplicationDbContext','IAutenticacionDatos')],
 'Infrastructure/ExpedienteService.cs':[('Requisitos(ApplicationDbContext db','Requisitos(IRequisitosDatos db'),('PdfStorage storage','ILectorArchivos storage'),('ValidarCatalogos(ApplicationDbContext db','ValidarCatalogos(ICatalogosDatos db')],
 'Infrastructure/RequisitosCatalogo.cs':[('this ApplicationDbContext db','this IRequisitosDatos db')],
 'Infrastructure/EvidenciaArchivo.cs':[('PdfStorage storage','IAlmacenPdf storage')],
 'Infrastructure/Permisos.cs':[('PermisoFilter(ApplicationDbContext db)','PermisoFilter(IPermisosDatos db)')]
}
for file,pairs in replacements.items():
 text=read(file)
 for old,new in pairs:
  assert old in text,(file,old);text=text.replace(old,new)
 write(file,text)
for folder in ['Controllers','solicitud-registro/Controllers']:
 for path in (root/folder).glob('*.cs'):
  text=path.read_text(encoding='utf8').replace('db.Database.BeginTransactionAsync(', 'db.BeginTransactionAsync(').replace('db.ChangeTracker.Clear()', 'db.DescartarCambios()')
  path.write_text(text,encoding='utf8')
# El predicado de seguridad es único para consultas y decisiones.
text=read('Controllers/GestionController.cs');visibility=method(text,'private IQueryable<Solicitud> Visibles()')
text=text.replace(visibility,'    private IQueryable<Solicitud> Visibles() => visibilidad.Visibles(Actor, User.FindFirstValue(ClaimTypes.Role) ?? "");')
write('Controllers/GestionController.cs',text)
# Reserva única, compartida por asignación y programación.
documents=read('Controllers/GestionDocumentos.cs');conflict=method(documents,'private async Task<bool> HayConflicto(')
documents=documents.replace(conflict+'\n','');write('Controllers/GestionDocumentos.cs',documents)
code=imports.replace('namespace SEDESLABORATORIO.Application.Tramites;','namespace SEDESLABORATORIO.Infrastructure;')
code+='public sealed class AgendaReservas(IGestionDatos db) : IAgendaReservas\n{\n'+conflict.replace('private async','public async')+'\n}\n'
write('Infrastructure/AgendaReservas.cs',code)

# Extraer responsabilidades reales del servicio general, manteniendo una fachada
# de compatibilidad para los ensayos y consumidores anteriores.
old=read('Infrastructure/TramiteService.cs')
parts={name:method(old,' '+name+'(') for name in ['PlazoHabil','Registrar','AvisarRol','AlertasVigencia','AvisoPlazo']}
for name,port,ctor,methods in [
 ('CalendarioLaboral','ICalendarioLaboral','ICatalogosDatos db',['PlazoHabil']),
 ('RegistroTramite','IRegistroTramite','IRegistroHistorialDatos db',['Registrar']),
 ('AvisosRol','IAvisosRol','IAvisosDatos db',['AvisarRol']),
 ('AlertasVigenciaService','IAlertasVigencia','IAlertasDatos db, IConfiguration? configuration, IReloj reloj',['AlertasVigencia','AvisoPlazo'])]:
 code=imports.replace('namespace SEDESLABORATORIO.Application.Tramites;','namespace SEDESLABORATORIO.Infrastructure;')+f'public sealed class {name}({ctor}) : {port}\n{{\n'
 code+='\n\n'.join(parts[m] for m in methods)+'\n}\n'
 code=code.replace('db.Database.BeginTransactionAsync(','db.BeginTransactionAsync(').replace('DateTime.UtcNow','reloj.UtcNow').replace('var today = HoraBolivia.Date','var today = reloj.HoraBolivia.Date')
 write('Infrastructure/'+name+'.cs',code)
facade='''using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Infrastructure;

// Fachada compatible con consumidores anteriores. El flujo de producción usa
// los contratos separados; aquí no se duplican reglas ni se confirman cambios.
public sealed class TramiteService(ApplicationDbContext db, IConfiguration? configuration = null)
{
    public static DateTime HoraBolivia => DateTime.UtcNow.AddHours(-4);
    public static DateTime DiasHabiles(DateTime fecha, int dias)
    {
        while (dias > 0) { fecha = fecha.AddDays(1); if (fecha.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) dias--; }
        return fecha.Date;
    }
    public Task<DateTime> PlazoHabil(DateTime fecha, int dias) => new CalendarioLaboral(db).PlazoHabil(fecha, dias);
    public void Registrar(Solicitud solicitud, int actorId, string detalle) => new RegistroTramite(db).Registrar(solicitud, actorId, detalle);
    public Task AvisarRol(RolUsuario rol, string mensaje) => new AvisosRol(db).AvisarRol(rol, mensaje);
    public Task AlertasVigencia() => new AlertasVigenciaService(db, configuration, new RelojSistema()).AlertasVigencia();
}

'''
worker=old[old.index('public sealed class VigenciaWorker'):].replace('GetRequiredService<TramiteService>()','GetRequiredService<IAlertasVigencia>()')
write('Infrastructure/TramiteService.cs',facade+worker)
print('Extraídas siete familias de acciones y cuatro servicios especializados.')

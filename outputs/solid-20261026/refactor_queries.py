from pathlib import Path
root=Path(__file__).resolve().parents[2]
def read(p):return (root/p).read_text(encoding='utf8')
def write(p,s):
 path=root/p;path.parent.mkdir(parents=True,exist_ok=True);path.write_text(s,encoding='utf8')
permissions={'EnviarSolicitud':'solicitudes','RevisarDocumentacion':'documentos','AsignarInspeccion':'asignaciones','ProgramarInspeccion':'inspecciones','RegistrarActa':'inspecciones','ValidarActa':'actas','ResolverLegal':'legal'}
for name,key in permissions.items():
 p='Application/Tramites/Acciones/'+name+'.cs';text=read(p);text=text.replace('    public IReadOnlyCollection<string> Acciones',f'    public string Permiso => "{key}";\n    public IReadOnlyCollection<string> Acciones');write(p,text)
p='Infrastructure/Permisos.cs';text=read(p);text='using SEDESLABORATORIO.Application.Tramites;\n'+text;text=text.replace('PermisoFilter(IPermisosDatos db)','PermisoFilter(IPermisosDatos db, RegistroAccionesTramite acciones)')
start=text.index('            key = model.Accion switch');end=text.index('        if (key is not null',start)
text=text[:start]+'            key = acciones.Buscar(model.Accion)?.Permiso;\n'+text[end:];write(p,text)

# Las métricas se calculan una vez y se presentan como HTML, JSON o CSV.
source=read('Controllers/MetricasController.cs')
start=source.index('        var exclusiveEnd = end.AddDays(1);');end=source.index('        if (!descargar)',start)
query=source[start:end]
visual=source[source.index('    private async Task<MetricasVisuales> CrearVisuales'):source.rfind('\n}')]
query=query.replace('        var exclusiveEnd = end.AddDays(1);','        var exclusiveEnd = end.AddDays(1);')
tail='''        var visuales = await CrearVisuales(solicitudes, rows, supervisores, borradores, start, end, municipio);
        return new MetricasModel { Desde = start, Hasta = end, Municipio = municipio, Filas = rows,
            Borradores = borradores, Supervisores = supervisores, Actividad = actividad, Visuales = visuales,
            MunicipiosDisponibles = await db.Solicitudes.AsNoTracking().Where(s => s.Municipio != "").Select(s => s.Municipio).Distinct().OrderBy(m => m).ToListAsync() };
'''
header='''using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Metricas;

public interface IConsultaMetricas
{
    Task<MetricasModel> Consultar(DateTime desde, DateTime hasta, string? municipio);
}

public sealed class ConsultaMetricas(IMetricasDatos db, IReloj reloj) : IConsultaMetricas
{
    public async Task<MetricasModel> Consultar(DateTime start, DateTime end, string? municipio)
    {
        if (end < start || end >= DateTime.MaxValue.Date || (end - start).TotalDays > 3660)
            throw new ArgumentException("El rango debe ser válido y no superar 10 años.");
'''
service=(header+query+tail+'    }\n\n'+visual+'\n}\n').replace('DateTime.UtcNow','reloj.UtcNow').replace('Infrastructure.TramiteService.HoraBolivia','reloj.HoraBolivia')
write('Application/Metricas/ConsultaMetricas.cs',service)
csvstart=source.index('        static string Csv(string value)');csvend=source.index('\n    }',csvstart)
csv=source[csvstart:csvend];csv=csv.replace('return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", "metricas.csv");','return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();')
export='''using System.Globalization;
using System.Text;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Metricas;

public interface IExportadorMetricas { byte[] Exportar(MetricasModel modelo); }

public sealed class ExportadorMetricasCsv : IExportadorMetricas
{
    public byte[] Exportar(MetricasModel modelo)
    {
        var rows = modelo.Filas; var borradores = modelo.Borradores; var supervisores = modelo.Supervisores;
        var actividad = modelo.Actividad; var start = modelo.Desde; var end = modelo.Hasta;
'''+csv+'\n    }\n}\n'
write('Application/Metricas/ExportadorMetricasCsv.cs',export)
controller=source[:start]+'''        var resultado = await consulta.Consultar(start, end, municipio);
        if (descargar) return File(exportador.Exportar(resultado), "text/csv; charset=utf-8", "metricas.csv");
        if (json) return Json(resultado.Visuales);
        return View("Index", resultado);
    }
}
'''
controller='using SEDESLABORATORIO.Application.Metricas;\n'+controller
controller=controller.replace('MetricasController(IMetricasDatos db)','MetricasController(IConsultaMetricas consulta, IExportadorMetricas exportador, IReloj reloj)').replace('DateTime.UtcNow','reloj.UtcNow')
write('Controllers/MetricasController.cs',controller)

# Consulta de agenda independiente de MVC. El adaptador conserva la hora civil.
p='Controllers/GestionSupervisor.cs';text=read(p);start=text.index('    private async Task<AgendaSupervisorModel>');end=text.index('    [HttpGet',start)
method=text[start:end].replace('private async Task<AgendaSupervisorModel> CargarAgenda(DateTime? fecha, bool rutas)','public async Task<AgendaSupervisorModel> Consultar(int actorId, DateTime? fecha, bool rutas)').replace('== Actor','== actorId').replace('TramiteService.HoraBolivia','reloj.HoraBolivia')
code='''using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public interface IConsultaAgenda
{
    Task<AgendaSupervisorModel> Consultar(int actorId, DateTime? fecha, bool rutas);
}

public sealed class ConsultaAgenda(IAgendaDatos db, IReloj reloj) : IConsultaAgenda
{
'''+method+'}\n';write('Application/Tramites/ConsultaAgenda.cs',code)
text=text[:start]+'    private Task<AgendaSupervisorModel> CargarAgenda(DateTime? fecha, bool rutas) => agendaSupervisor.Consultar(Actor, fecha, rutas);\n\n'+text[end:];write(p,text)
p='Controllers/GestionController.cs';text=read(p).replace('IVisibilidadTramites visibilidad)','IVisibilidadTramites visibilidad, IConsultaAgenda agendaSupervisor)');write(p,text)
print('Consultas, exportación, agenda y permisos separados.')

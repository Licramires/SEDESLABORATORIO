using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SEDESLABORATORIO.Modules.SolicitudRegistro.Models; // Importamos tu ViewModel

namespace SEDESLABORATORIO.SolicitudRegistro.Controllers;

[Area("solicitud-registro")]
public class SolicitudRegistroController : Controller
{
    // Este es el que ya tenías, lo dejamos por si acaso
    public IActionResult Index() => View();

    // GET: Mostrará la vista del formulario vacío para la nueva solicitud
    [HttpGet]
    public IActionResult Nueva()
    {
        var modelo = new NuevaSolicitudViewModel();
        return View(modelo);
    }

    // POST: Recibirá los datos y documentos cuando el usuario envíe el formulario
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Nueva(NuevaSolicitudViewModel modelo)
    {
        if (ModelState.IsValid)
        {
            // Más adelante aquí agregaremos la lógica para guardar los datos 
            // en la base de datos compartida y procesar los PDFs.
            
            // Por ahora, si todo es válido, lo mandaremos a una pantalla de éxito
            return RedirectToAction("Exito");
        }

        // Si falta algún dato requerido, vuelve a mostrar el formulario con los errores
        return View(modelo);
    }

    // GET: Vista de confirmación de éxito
    [HttpGet]
    public IActionResult Exito()
    {
        return View();
    }
}
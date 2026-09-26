using GestionCreditos.Data;
using GestionCreditos.Models;
using GestionCreditos.Models.ViewModels;
using GestionCreditos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionCreditos.Areas.Operaciones.Controllers;

[Area("Operaciones")]
public class IncidenciasController(
    IIncidenciaListaCacheService listaCache,
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Listado general: se sirve desde la caché distribuida (Redis)
        // de 60 segundos; el servicio registra si vino de caché o de BD.
        var viewModel = new IncidenciasIndexViewModel
        {
            Incidencias = await listaCache.GetAbiertasAsync(HttpContext.RequestAborted)
        };
        return View(viewModel);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await db.Incidencias.FindAsync(id);
        if (incidencia is null)
        {
            return NotFound();
        }

        if (incidencia.Estado == EstadoIncidencia.Abierta)
        {
            incidencia.Estado = EstadoIncidencia.Cerrada;
            await db.SaveChangesAsync();

            // Invalida la caché del listado antes de volver a consultar
            // los datos (el RedirectToAction provoca una lectura actualizada).
            await listaCache.InvalidarAsync(incidencia.Id);
            TempData["Mensaje"] = $"Incidencia #{incidencia.Id} cerrada correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}

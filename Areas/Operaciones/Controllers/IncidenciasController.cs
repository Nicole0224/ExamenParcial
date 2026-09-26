using GestionCreditos.Data;
using GestionCreditos.Models;
using GestionCreditos.Models.ViewModels;
using GestionCreditos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCreditos.Areas.Operaciones.Controllers;

[Area("Operaciones")]
public class IncidenciasController(
    ApplicationDbContext db,
    IIncidenciaSearchService searchService,
    IIncidenciaListaCacheService listaCache,
    ILogger<IncidenciasController> logger) : Controller
{
    public async Task<IActionResult> Index([FromQuery] string? q)
    {
        var viewModel = new IncidenciasIndexViewModel { Q = q };

        if (string.IsNullOrWhiteSpace(q))
        {
            // Listado general: se sirve desde la caché distribuida (Redis)
            // de 60 segundos; el servicio registra si vino de caché o de BD.
            viewModel.Incidencias = await listaCache.GetAbiertasAsync(HttpContext.RequestAborted);
            return View(viewModel);
        }

        // Búsqueda con texto: el servidor consulta Algolia directamente,
        // sin usar la caché, y muestra solo las coincidencias que
        // continúan abiertas en la base de datos.
        try
        {
            var ids = await searchService.BuscarIdsAsync(q.Trim(), HttpContext.RequestAborted);
            viewModel.Incidencias = await db.Incidencias
                .Where(i => i.Estado == EstadoIncidencia.Abierta && ids.Contains(i.Id))
                .OrderByDescending(i => i.FechaReporte)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al buscar incidencias en Algolia.");
            viewModel.Error = "La búsqueda no está disponible en este momento. Intente de nuevo más tarde.";
        }

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

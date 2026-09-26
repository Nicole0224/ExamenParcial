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
    IPieHostEventPublisher eventPublisher,
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
        }
        else
        {
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
        }

        viewModel.TiempoRealWsUrl = eventPublisher.UrlSuscripcion;
        viewModel.ListaUrl = Url.Action(nameof(Lista)) ?? string.Empty;
        return View(viewModel);
    }

    /// <summary>
    /// Estado vigente de las incidencias abiertas en JSON. Lo usa la
    /// pantalla para reconsultar al reconectarse al WebSocket.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        var abiertas = await db.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(i => i.FechaReporte)
            .Select(i => new
            {
                i.Id,
                i.Estacion,
                i.Descripcion,
                Prioridad = i.Prioridad.ToString(),
                FechaReporte = i.FechaReporte
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Json(abiertas);
    }

    [HttpPost]
    [Authorize(Roles = "Supervisor")]
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
            // 1. Guardar primero el nuevo estado en la base de datos.
            incidencia.Estado = EstadoIncidencia.Cerrada;
            await db.SaveChangesAsync();

            // 2. Invalidar la caché del listado (la próxima lectura
            // irá a la base de datos ya actualizada).
            await listaCache.InvalidarAsync(incidencia.Id);

            // 3. Publicar después el evento en PieHost (no revierte el cierre).
            try
            {
                await eventPublisher.PublicarIncidenciaActualizadaAsync(
                    incidencia.Id,
                    incidencia.Estado.ToString(),
                    HttpContext.RequestAborted);
                TempData["Mensaje"] = $"Incidencia #{incidencia.Id} cerrada correctamente.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo publicar el evento IncidenciaActualizada para #{IncidenciaId}.", incidencia.Id);
                TempData["Mensaje"] = $"Incidencia #{incidencia.Id} cerrada correctamente.";
                TempData["Aviso"] = "El cierre se guardó, pero no se pudo notificar en tiempo real.";
            }
        }

        return RedirectToAction(nameof(Index));
    }
}

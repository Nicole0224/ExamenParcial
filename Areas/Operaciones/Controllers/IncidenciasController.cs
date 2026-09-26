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
    IPieHostEventPublisher eventPublisher,
    ILogger<IncidenciasController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var abiertas = await db.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync(HttpContext.RequestAborted);

        return View(new IncidenciasIndexViewModel
        {
            Incidencias = abiertas,
            TiempoRealWsUrl = eventPublisher.UrlSuscripcion,
            ListaUrl = Url.Action(nameof(Lista)) ?? string.Empty
        });
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

            // 2. Publicar después el evento en PieHost (no revierte el cierre).
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

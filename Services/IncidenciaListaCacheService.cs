using System.Text.Json;
using GestionCreditos.Data;
using GestionCreditos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace GestionCreditos.Services;

/// <summary>
/// Lista general de incidencias abiertas con caché distribuida (Redis)
/// de 60 segundos. Registra en logs si cada lectura vino de la caché
/// o de la base de datos, y permite invalidar la clave al cerrar
/// una incidencia.
/// </summary>
public sealed class IncidenciaListaCacheService(
    ApplicationDbContext db,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<IncidenciaListaCacheService> logger) : IIncidenciaListaCacheService
{
    public const string CacheKey = "incidencias:abiertas";

    private static readonly TimeSpan DuracionCache = TimeSpan.FromSeconds(60);

    private readonly bool _esRedis = !string.IsNullOrWhiteSpace(configuration["Redis:ConnectionString"]);

    private string Origen => _esRedis ? "Redis" : "la caché en memoria";

    public async Task<List<Incidencia>> GetAbiertasAsync(CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetStringAsync(CacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            logger.LogInformation("Lista de incidencias abiertas obtenida desde {Origen}.", Origen);
            return JsonSerializer.Deserialize<List<Incidencia>>(cached) ?? [];
        }

        var abiertas = await db.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(i => i.FechaReporte)
            .ToListAsync(cancellationToken);

        await cache.SetStringAsync(
            CacheKey,
            JsonSerializer.Serialize(abiertas),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DuracionCache },
            cancellationToken);

        logger.LogInformation(
            "Lista de incidencias abiertas obtenida desde la base de datos; guardada en caché por 60 segundos.");
        return abiertas;
    }

    public async Task InvalidarAsync(int incidenciaId, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(CacheKey, cancellationToken);
        logger.LogInformation(
            "Caché de la lista de incidencias abiertas invalidada tras cerrar la incidencia #{IncidenciaId}.",
            incidenciaId);
    }
}

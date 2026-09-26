using GestionCreditos.Models;

namespace GestionCreditos.Services;

/// <summary>
/// Caché de la lista general de incidencias abiertas (60 segundos).
/// Solo el listado general usa caché; las búsquedas por texto consultan
/// Algolia directamente sin pasar por aquí.
/// </summary>
public interface IIncidenciaListaCacheService
{
    Task<List<Incidencia>> GetAbiertasAsync(CancellationToken cancellationToken = default);

    Task InvalidarAsync(int incidenciaId, CancellationToken cancellationToken = default);
}

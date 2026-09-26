namespace GestionCreditos.Services;

/// <summary>
/// Búsqueda de incidencias en el índice de Algolia. Se ejecuta
/// exclusivamente en el servidor: la API key nunca sale al navegador.
/// Devuelve los Ids (objectID) de los registros coincidentes.
/// </summary>
public interface IIncidenciaSearchService
{
    Task<IReadOnlyList<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default);
}

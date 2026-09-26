using System.Text;
using System.Text.Json;

namespace GestionCreditos.Services;

/// <summary>
/// Consulta el índice de Algolia mediante su API REST directamente desde
/// el servidor. La clave de Algolia (incluso la administrativa) solo se usa
/// aquí y jamás se envía al navegador: el cliente únicamente envía el texto
/// a buscar al controlador.
/// Configuración: Algolia:AppId, Algolia:ApiKey, Algolia:IndexName
/// (variables de entorno Algolia__AppId, Algolia__ApiKey, Algolia__IndexName).
/// </summary>
public sealed class AlgoliaIncidenciaSearchService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AlgoliaIncidenciaSearchService> logger) : IIncidenciaSearchService
{
    public async Task<IReadOnlyList<int>> BuscarIdsAsync(string texto, CancellationToken cancellationToken = default)
    {
        var appId = configuration["Algolia:AppId"];
        var apiKey = configuration["Algolia:ApiKey"];
        var indexName = configuration["Algolia:IndexName"];

        if (string.IsNullOrWhiteSpace(appId)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(indexName))
        {
            throw new InvalidOperationException(
                "Falta la configuración de Algolia (Algolia:AppId, Algolia:ApiKey, Algolia:IndexName).");
        }

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://{appId}-dsn.algolia.net/1/indexes/{indexName}/query");
        request.Headers.Add("X-Algolia-Application-Id", appId);
        request.Headers.Add("X-Algolia-API-Key", apiKey);

        var body = JsonSerializer.Serialize(new
        {
            @params = $"query={Uri.EscapeDataString(texto)}&hitsPerPage=100"
        });
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var ids = new List<int>();
        if (doc.RootElement.TryGetProperty("hits", out var hits))
        {
            foreach (var hit in hits.EnumerateArray())
            {
                if (!hit.TryGetProperty("objectID", out var objectId))
                {
                    continue;
                }

                if (objectId.ValueKind == JsonValueKind.Number && objectId.TryGetInt32(out var numericId))
                {
                    ids.Add(numericId);
                }
                else if (objectId.ValueKind == JsonValueKind.String
                    && int.TryParse(objectId.GetString(), out var parsedId))
                {
                    ids.Add(parsedId);
                }
                else
                {
                    logger.LogWarning("Hit de Algolia con objectID no numérico ignorado.");
                }
            }
        }

        return ids;
    }
}

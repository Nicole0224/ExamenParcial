using System.Text;
using System.Text.Json;

namespace GestionCreditos.Services;

/// <summary>
/// Publica eventos en PieSocket mediante su API REST directamente desde
/// el servidor (sin dependencias externas). El secreto viaja solo en el
/// cuerpo de la petición servidor→PieSocket y jamás se expone al navegador.
/// Configuración: PieHost:ClusterId, PieHost:ApiKey, PieHost:ApiSecret,
/// PieHost:Channel (variables PieHost__ClusterId, PieHost__ApiKey,
/// PieHost__ApiSecret, PieHost__Channel).
/// </summary>
public sealed class PieHostEventPublisher(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PieHostEventPublisher> logger) : IPieHostEventPublisher
{
    public const string NombreEvento = "IncidenciaActualizada";

    private string? ClusterId => configuration["PieHost:ClusterId"];
    private string? ApiKey => configuration["PieHost:ApiKey"];
    private string? ApiSecret => configuration["PieHost:ApiSecret"];
    private string? Canal => configuration["PieHost:Channel"];

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(ClusterId)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret)
        && !string.IsNullOrWhiteSpace(Canal);

    public string? UrlSuscripcion =>
        EstaConfigurado
            ? $"wss://{ClusterId}.piesocket.com/v3/{Canal}?api_key={ApiKey}&notify_self=1"
            : null;

    public async Task PublicarIncidenciaActualizadaAsync(int id, string estado, CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado)
        {
            throw new InvalidOperationException(
                "Falta la configuración de PieHost (PieHost:ClusterId, PieHost:ApiKey, PieHost:ApiSecret, PieHost:Channel).");
        }

        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://{ClusterId}.piesocket.com/api/publish");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                key = ApiKey,
                secret = ApiSecret,
                channelId = Canal,
                message = new
                {
                    @event = NombreEvento,
                    data = new { Id = id, Estado = estado }
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation(
            "Evento {Evento} publicado en PieHost para la incidencia #{IncidenciaId} con estado {Estado}.",
            NombreEvento, id, estado);
    }
}

namespace GestionCreditos.Services;

/// <summary>
/// Publica eventos de incidencias en el canal WebSocket de PieHost.
/// Se ejecuta exclusivamente en el servidor: el secreto de PieHost
/// nunca sale al navegador (el cliente solo recibe la URL pública
/// de suscripción con la API key).
/// </summary>
public interface IPieHostEventPublisher
{
    /// <summary>
    /// Indica si la publicación está configurada (cluster, key, secret y canal).
    /// </summary>
    bool EstaConfigurado { get; }

    /// <summary>
    /// URL pública de suscripción al canal (sin el secreto), para la vista.
    /// Null si no está configurado.
    /// </summary>
    string? UrlSuscripcion { get; }

    Task PublicarIncidenciaActualizadaAsync(int id, string estado, CancellationToken cancellationToken = default);
}

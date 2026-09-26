namespace GestionCreditos.Models.ViewModels;

public class IncidenciasIndexViewModel
{
    public string? Q { get; set; }

    public List<Incidencia> Incidencias { get; set; } = [];

    public string? Error { get; set; }

    /// <summary>
    /// URL pública del canal WebSocket (solo host, canal y API key pública;
    /// jamás incluye el secreto). Null si el tiempo real no está configurado.
    /// </summary>
    public string? TiempoRealWsUrl { get; set; }

    /// <summary>
    /// Endpoint JSON para reconsultar el estado vigente al reconectar.
    /// </summary>
    public string ListaUrl { get; set; } = string.Empty;
}

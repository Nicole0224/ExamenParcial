namespace GestionCreditos.Models.ViewModels;

public class IncidenciasIndexViewModel
{
    public string? Q { get; set; }

    public List<Incidencia> Incidencias { get; set; } = [];

    public string? Error { get; set; }
}

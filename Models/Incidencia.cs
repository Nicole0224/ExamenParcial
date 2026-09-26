namespace GestionCreditos.Models;

public enum EstadoIncidencia
{
    Abierta,
    Cerrada
}

public enum PrioridadIncidencia
{
    Baja,
    Media,
    Alta
}

public class Incidencia
{
    public int Id { get; set; }

    public string Estacion { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public PrioridadIncidencia Prioridad { get; set; } = PrioridadIncidencia.Media;

    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    public DateTime FechaReporte { get; set; } = DateTime.UtcNow;
}

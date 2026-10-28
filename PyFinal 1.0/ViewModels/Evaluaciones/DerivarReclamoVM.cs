namespace PyFinal_1.ViewModels.Evaluaciones;

public class DerivarReclamoVM
{
    public string Codigo { get; set; } = string.Empty;
    public string TipoReclamo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    public List<EncargadoItemVM> Encargados { get; set; } = new();
    public int? EncargadoId { get; set; }

    public bool HayDisponibles => Encargados.Any(e => e.Disponible);

    // Respuesta del patrón State
    public bool PuedeDerivar { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

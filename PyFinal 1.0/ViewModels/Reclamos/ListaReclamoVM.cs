using PyFinal_1.Domain.Enums;

namespace PyFinal_1.ViewModels.Reclamos;

public class ListaReclamoVM
{
    public string Codigo { get; set; } = string.Empty;
    public string TipoReclamo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public EstadoReclamo Estado { get; set; }
}
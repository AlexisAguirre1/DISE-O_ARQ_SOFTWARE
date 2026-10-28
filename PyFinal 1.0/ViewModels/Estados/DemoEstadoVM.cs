using PyFinal_1.Domain.Enums;

namespace PyFinal_1.ViewModels.Estados;

public class DemoEstadoVM
{
   
    public EstadoReclamo EstadoInicial { get; set; } = EstadoReclamo.Registrado;
    public string Accion { get; set; } = "Evaluar";
    public bool HayOperarioLibre { get; set; }

    
    public bool Ejecutado { get; set; }
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public EstadoReclamo EstadoFinal { get; set; }
}
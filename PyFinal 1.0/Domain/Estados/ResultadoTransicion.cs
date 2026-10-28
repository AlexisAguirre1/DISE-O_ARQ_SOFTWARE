using PyFinal_1.Domain.Enums;

namespace PyFinal_1.Domain.Estados;

public class ResultadoTransicion
{
    public bool Exito { get; }
    public string Mensaje { get; }
    public EstadoReclamo? NuevoEstado { get; }

    private ResultadoTransicion(bool exito, string mensaje, EstadoReclamo? nuevoEstado)
    {
        Exito = exito;
        Mensaje = mensaje;
        NuevoEstado = nuevoEstado;
    }

    public static ResultadoTransicion Ok(EstadoReclamo nuevoEstado, string mensaje)
        => new(true, mensaje, nuevoEstado);

    public static ResultadoTransicion Rechazada(string mensaje)
        => new(false, mensaje, null);
}
using PyFinal_1.Domain.Enums;

namespace PyFinal_1.Domain.Estados;

public interface IEstadoReclamo
{
    EstadoReclamo Estado { get; }

    ResultadoTransicion Evaluar();
    ResultadoTransicion Derivar(bool hayOperarioLibre);
    ResultadoTransicion Resolver();
    ResultadoTransicion Cerrar();
}
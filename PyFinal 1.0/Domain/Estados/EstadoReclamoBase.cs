using PyFinal_1.Domain.Enums;

namespace PyFinal_1.Domain.Estados;

public abstract class EstadoReclamoBase : IEstadoReclamo
{
    public abstract EstadoReclamo Estado { get; }

    public virtual ResultadoTransicion Evaluar() => NoPermitido("evaluar");
    public virtual ResultadoTransicion Derivar(bool hayOperarioLibre) => NoPermitido("derivar");
    public virtual ResultadoTransicion Resolver() => NoPermitido("resolver");
    public virtual ResultadoTransicion Cerrar() => NoPermitido("cerrar");

    protected ResultadoTransicion NoPermitido(string accion)
        => ResultadoTransicion.Rechazada(
            $"No se puede {accion} un reclamo en estado {Estado}.");            
}
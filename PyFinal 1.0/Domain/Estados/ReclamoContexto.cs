using PyFinal_1.Domain.Enums;

namespace PyFinal_1.Domain.Estados;

public class ReclamoContexto
{
    private IEstadoReclamo _estado;

    public ReclamoContexto(EstadoReclamo estadoInicial)
    {
        _estado = CrearEstado(estadoInicial);
    }

    public EstadoReclamo EstadoActual => _estado.Estado;

    public ResultadoTransicion Evaluar() => Aplicar(_estado.Evaluar());
    public ResultadoTransicion Derivar(bool hayOperarioLibre) => Aplicar(_estado.Derivar(hayOperarioLibre));
    public ResultadoTransicion Resolver() => Aplicar(_estado.Resolver());
    public ResultadoTransicion Cerrar() => Aplicar(_estado.Cerrar());

    // Si la transición fue exitosa, el contexto cambia de estado
    private ResultadoTransicion Aplicar(ResultadoTransicion resultado)
    {
        if (resultado.Exito && resultado.NuevoEstado.HasValue)
            _estado = CrearEstado(resultado.NuevoEstado.Value);

        return resultado;
    }

    // Único lugar del sistema que conoce la correspondencia enum -> clase
    private static IEstadoReclamo CrearEstado(EstadoReclamo estado) => estado switch
    {
        EstadoReclamo.Registrado => new EstadoRegistrado(),
        EstadoReclamo.EnEvaluacion => new EstadoEnEvaluacion(),
        EstadoReclamo.EnProceso => new EstadoEnProceso(),
        EstadoReclamo.Resuelto => new EstadoResuelto(),
        EstadoReclamo.Cerrado => new EstadoCerrado(),
        _ => new EstadoSinTransiciones(estado)
    };
}
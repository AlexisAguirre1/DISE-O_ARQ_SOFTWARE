using PyFinal_1.Domain.Enums;

namespace PyFinal_1.Domain.Estados;

public class EstadoRegistrado : EstadoReclamoBase
{
    public override EstadoReclamo Estado => EstadoReclamo.Registrado;

    public override ResultadoTransicion Evaluar()
        => ResultadoTransicion.Ok(EstadoReclamo.EnEvaluacion,
            "El reclamo pasó a evaluación.");
}

public class EstadoEnEvaluacion : EstadoReclamoBase
{
    public override EstadoReclamo Estado => EstadoReclamo.EnEvaluacion;

    // Si hay un operario libre del tipo se deriva,
    // Si no, el reclamo se queda en evaluación.
    public override ResultadoTransicion Derivar(bool hayOperarioLibre)
        => hayOperarioLibre
            ? ResultadoTransicion.Ok(EstadoReclamo.EnProceso,
                "Reclamo derivado al operario.")
            : ResultadoTransicion.Rechazada(
                "No hay operarios libres de este tipo. El reclamo permanece en evaluación.");
}

public class EstadoEnProceso : EstadoReclamoBase
{
    public override EstadoReclamo Estado => EstadoReclamo.EnProceso;

    public override ResultadoTransicion Resolver()
        => ResultadoTransicion.Ok(EstadoReclamo.Resuelto,
            "El reclamo fue resuelto.");
}

public class EstadoResuelto : EstadoReclamoBase
{
    public override EstadoReclamo Estado => EstadoReclamo.Resuelto;

    public override ResultadoTransicion Cerrar()
        => ResultadoTransicion.Ok(EstadoReclamo.Cerrado,
            "El reclamo fue cerrado.");
}

// Estado final: no permite ninguna acción (todo queda prohibido por la clase base)
public class EstadoCerrado : EstadoReclamoBase
{
    public override EstadoReclamo Estado => EstadoReclamo.Cerrado;
}

// Para Aprobado, Rechazado, Impugnado y Desconocido, que aún no tienen transiciones definidas
public class EstadoSinTransiciones : EstadoReclamoBase
{
    private readonly EstadoReclamo _estado;

    public EstadoSinTransiciones(EstadoReclamo estado) => _estado = estado;

    public override EstadoReclamo Estado => _estado;
}
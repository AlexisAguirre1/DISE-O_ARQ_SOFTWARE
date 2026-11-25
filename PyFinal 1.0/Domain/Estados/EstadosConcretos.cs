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

    // Aprobar un reclamo equivale a derivarlo a un operario libre del tipo.
    // Si no hay ninguno libre, el reclamo permanece en evaluación.
    public override ResultadoTransicion Derivar(bool hayOperarioLibre)
        => hayOperarioLibre
            ? ResultadoTransicion.Ok(EstadoReclamo.EnProceso,
                "Reclamo derivado al operario.")
            : ResultadoTransicion.Rechazada(
                "No hay operarios libres de este tipo. El reclamo permanece en evaluación.");

    public override ResultadoTransicion Rechazar()
        => ResultadoTransicion.Ok(EstadoReclamo.Rechazado,
            "El reclamo fue rechazado.");
}

// Reclamo rechazado por el supervisor: el solicitante puede impugnarlo
public class EstadoRechazado : EstadoReclamoBase
{
    public override EstadoReclamo Estado => EstadoReclamo.Rechazado;

    public override ResultadoTransicion Impugnar()
        => ResultadoTransicion.Ok(EstadoReclamo.Impugnado,
            "La impugnación fue registrada.");
}

// Reclamo impugnado: el supervisor decide si acepta o rechaza la impugnación
public class EstadoImpugnado : EstadoReclamoBase
{
    public override EstadoReclamo Estado => EstadoReclamo.Impugnado;

    public override ResultadoTransicion AceptarImpugnacion()
        => ResultadoTransicion.Ok(EstadoReclamo.EnEvaluacion,
            "Impugnación aceptada: el reclamo vuelve a evaluación.");

    public override ResultadoTransicion RechazarImpugnacion()
        => ResultadoTransicion.Ok(EstadoReclamo.Rechazado,
            "Impugnación rechazada: se mantiene el rechazo.");
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

// Para los estados que aún no tienen transiciones definidas (Aprobado, Desconocido)
public class EstadoSinTransiciones : EstadoReclamoBase
{
    private readonly EstadoReclamo _estado;

    public EstadoSinTransiciones(EstadoReclamo estado) => _estado = estado;

    public override EstadoReclamo Estado => _estado;
}
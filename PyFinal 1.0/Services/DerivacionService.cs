using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Domain.Enums;
using PyFinal_1.Domain.Estados;
using PyFinal_1.Models;
using PyFinal_1.ViewModels.Evaluaciones;
using PyFinal_1.ViewModels.Reclamos;

namespace PyFinal_1.Services;

public class DerivacionService
{
    private const int IdEstadoEnEvaluacion = 2;
    private const int IdEstadoEnProceso = 3;
    private const int IdRolSupervisor = 2;

    private readonly ReclamosDbContext _db;

    public DerivacionService(ReclamosDbContext db)
    {
        _db = db;
    }

    // Reclamos que esperan ser derivados (estado "En evaluacion")
    public async Task<List<ListaReclamoVM>> ObtenerPendientesAsync()
    {
        var datos = await (
            from r in _db.Reclamos
            join t in _db.TiposReclamo on r.IdTipoReclamo equals t.IdTipoReclamo
            where r.IdEstado == IdEstadoEnEvaluacion
            orderby r.FechaRegistro
            select new { r.CodigoReclamo, t.NombreTipo, r.Detalle, r.FechaRegistro }
        ).ToListAsync();

        return datos.Select(d => new ListaReclamoVM
        {
            Codigo = d.CodigoReclamo,
            TipoReclamo = d.NombreTipo,
            Descripcion = d.Detalle,
            FechaRegistro = d.FechaRegistro ?? DateTime.Now,
            Estado = EstadoReclamo.EnEvaluacion
        }).ToList();
    }

    // Reclamo + operarios de su tipo, cada uno marcado como libre u ocupado
    public async Task<DerivarReclamoVM?> ObtenerParaDerivarAsync(string codigo)
    {
        var reclamo = await (
            from r in _db.Reclamos
            join t in _db.TiposReclamo on r.IdTipoReclamo equals t.IdTipoReclamo
            where r.CodigoReclamo == codigo
            select new
            {
                r.CodigoReclamo,
                r.IdTipoReclamo,
                t.NombreTipo,
                r.Detalle,
                r.FechaRegistro,
                r.IdEstado
            }
        ).FirstOrDefaultAsync();

        if (reclamo == null) return null;

        // Un operario está ocupado si tiene un reclamo "En proceso" asignado
        var operarios = await (
            from o in _db.Operarios
            join u in _db.Usuarios on o.IdUsuario equals u.IdUsuario
            where o.IdTipoReclamo == reclamo.IdTipoReclamo
            orderby u.NombresApellidos
            select new EncargadoItemVM
            {
                Id = o.IdUsuario,
                Nombre = u.NombresApellidos,
                TipoEncargado = reclamo.NombreTipo,
                Disponible = !_db.Reclamos.Any(x =>
                    x.IdOperario == o.IdUsuario && x.IdEstado == IdEstadoEnProceso)
            }
        ).ToListAsync();

        // Patrón State: el estado actual decide si se puede derivar.
        var contexto = new ReclamoContexto(MapearEstado(reclamo.IdEstado));
        var resultado = contexto.Derivar(operarios.Any(o => o.Disponible));

        return new DerivarReclamoVM
        {
            Codigo = reclamo.CodigoReclamo,
            TipoReclamo = reclamo.NombreTipo,
            Descripcion = reclamo.Detalle,
            FechaRegistro = reclamo.FechaRegistro ?? DateTime.Now,
            Encargados = operarios,
            PuedeDerivar = resultado.Exito,
            Mensaje = resultado.Exito
                ? "Hay operarios libres: el reclamo puede derivarse."
                : resultado.Mensaje
        };
    }

    // Pasa un reclamo de "Registrado" a "En evaluación"
    public async Task<(bool Exito, string Mensaje)> EvaluarAsync(string codigo, int idSupervisor)
    {
        var reclamo = await _db.Reclamos
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.CodigoReclamo == codigo);

        if (reclamo == null)
            return (false, "El reclamo no existe.");

        // Patrón State: el estado actual decide si se puede evaluar
        var contexto = new ReclamoContexto(MapearEstado(reclamo.IdEstado));
        var resultado = contexto.Evaluar();

        if (!resultado.Exito)
            return (false, resultado.Mensaje);

        int estadoAnterior = reclamo.IdEstado;
        int estadoNuevo = MapearIdEstado(resultado.NuevoEstado!.Value);
        DateTime ahora = DateTime.Now;

        await using var transaccion = await _db.Database.BeginTransactionAsync();

        // Solo actualiza si nadie más lo cambió mientras tanto
        int actualizados = await _db.Reclamos
            .Where(r => r.IdReclamo == reclamo.IdReclamo
                        && r.IdEstado == estadoAnterior)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IdEstado, estadoNuevo)
                .SetProperty(r => r.FechaActualizacion, (DateTime?)ahora));

        if (actualizados != 1)
            return (false, "El reclamo cambió mientras lo evaluabas. Actualiza la lista.");

        _db.HistorialReclamos.Add(new HistorialReclamoBD
        {
            IdReclamo = reclamo.IdReclamo,
            IdEstadoAnterior = estadoAnterior,
            IdEstadoNuevo = estadoNuevo,
            IdUsuarioModificacion = idSupervisor,
            Comentario = "Reclamo tomado para evaluación",
            FechaCambio = ahora
        });

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return (true, $"Reclamo {codigo} en evaluación.");
    }

    // Guarda la derivación: actualiza el reclamo y escribe el historial
    // Aprobar un reclamo = derivarlo a un operario libre.
    // Actualiza el reclamo y registra la evaluación y el historial.
    public async Task<(bool Exito, string Mensaje)> DerivarAsync(
        string codigo, int operarioId, int idSupervisor)
    {
        var reclamo = await _db.Reclamos
            .FirstOrDefaultAsync(r => r.CodigoReclamo == codigo);

        if (reclamo == null)
            return (false, "El reclamo no existe.");

        // El operario debe existir y ser del tipo del reclamo
        var operario = await (
            from o in _db.Operarios
            join u in _db.Usuarios on o.IdUsuario equals u.IdUsuario
            where o.IdUsuario == operarioId && o.IdTipoReclamo == reclamo.IdTipoReclamo
            select new { o.IdUsuario, u.NombresApellidos }
        ).FirstOrDefaultAsync();

        if (operario == null)
            return (false, "El operario no corresponde al tipo de este reclamo.");

        // Debe estar libre (se vuelve a comprobar al guardar)
        bool ocupado = await _db.Reclamos.AnyAsync(x =>
            x.IdOperario == operarioId && x.IdEstado == IdEstadoEnProceso);

        if (ocupado)
            return (false, $"{operario.NombresApellidos} ya está ocupado con otro reclamo.");

        // Patrón State: decide si la transición es válida
        var contexto = new ReclamoContexto(MapearEstado(reclamo.IdEstado));
        var resultado = contexto.Derivar(true);

        if (!resultado.Exito)
            return (false, resultado.Mensaje);

        int estadoAnterior = reclamo.IdEstado;
        int estadoNuevo = MapearIdEstado(resultado.NuevoEstado!.Value);
        DateTime ahora = DateTime.Now;

        reclamo.IdOperario = operarioId;
        reclamo.IdEstado = estadoNuevo;
        reclamo.FechaActualizacion = ahora;

        // Queda registrada la decisión del supervisor
        _db.Evaluaciones.Add(new EvaluacionBD
        {
            IdReclamo = reclamo.IdReclamo,
            IdSupervisor = idSupervisor,
            Resultado = "Aprobado",
            Respuesta = null,
            FechaEvaluacion = ahora
        });

        _db.HistorialReclamos.Add(new HistorialReclamoBD
        {
            IdReclamo = reclamo.IdReclamo,
            IdEstadoAnterior = estadoAnterior,
            IdEstadoNuevo = estadoNuevo,
            IdUsuarioModificacion = idSupervisor,
            Comentario = $"Aprobado y derivado a {operario.NombresApellidos}",
            FechaCambio = ahora
        });

        // Un solo SaveChanges: reclamo, evaluación e historial
        // se guardan juntos o no se guarda nada
        await _db.SaveChangesAsync();

        return (true, $"Reclamo {codigo} derivado a {operario.NombresApellidos}.");
    }

    // El supervisor rechaza el reclamo y deja una respuesta para el solicitante
    public async Task<(bool Exito, string Mensaje)> RechazarAsync(
        string codigo, string? respuesta, int idSupervisor)
    {
        respuesta = respuesta?.Trim() ?? string.Empty;

        if (respuesta.Length < 10)
            return (false, "Escribe una respuesta de al menos 10 caracteres para el solicitante.");

        if (respuesta.Length > 500)
            return (false, "La respuesta no puede superar los 500 caracteres.");

        var reclamo = await _db.Reclamos
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.CodigoReclamo == codigo);

        if (reclamo == null)
            return (false, "El reclamo no existe.");

        // Patrón State: solo se puede rechazar un reclamo en evaluación
        var contexto = new ReclamoContexto(MapearEstado(reclamo.IdEstado));
        var resultado = contexto.Rechazar();

        if (!resultado.Exito)
            return (false, resultado.Mensaje);

        int estadoAnterior = reclamo.IdEstado;
        int estadoNuevo = MapearIdEstado(resultado.NuevoEstado!.Value);
        DateTime ahora = DateTime.Now;

        await using var transaccion = await _db.Database.BeginTransactionAsync();

        // Solo actualiza si nadie más lo cambió mientras tanto
        int actualizados = await _db.Reclamos
            .Where(r => r.IdReclamo == reclamo.IdReclamo
                        && r.IdEstado == estadoAnterior
                        && r.IdOperario == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IdEstado, estadoNuevo)
                .SetProperty(r => r.FechaActualizacion, (DateTime?)ahora));

        if (actualizados != 1)
            return (false, "El reclamo cambió mientras lo rechazabas. Actualiza la lista.");

        _db.Evaluaciones.Add(new EvaluacionBD
        {
            IdReclamo = reclamo.IdReclamo,
            IdSupervisor = idSupervisor,
            Resultado = "Rechazado",
            Respuesta = respuesta,
            FechaEvaluacion = ahora
        });

        _db.HistorialReclamos.Add(new HistorialReclamoBD
        {
            IdReclamo = reclamo.IdReclamo,
            IdEstadoAnterior = estadoAnterior,
            IdEstadoNuevo = estadoNuevo,
            IdUsuarioModificacion = idSupervisor,
            Comentario = "Reclamo rechazado por el supervisor.",
            FechaCambio = ahora
        });

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return (true, $"Reclamo {codigo} rechazado. El solicitante verá tu respuesta.");
    }

    private static EstadoReclamo MapearEstado(int idEstado) => idEstado switch
    {
        1 => EstadoReclamo.Registrado,
        2 => EstadoReclamo.EnEvaluacion,
        3 => EstadoReclamo.EnProceso,
        4 => EstadoReclamo.Resuelto,
        5 => EstadoReclamo.Cerrado,
        6 => EstadoReclamo.Rechazado,
        7 => EstadoReclamo.Impugnado,
        _ => throw new InvalidOperationException($"Estado {idEstado} no reconocido.")
    };

    private static int MapearIdEstado(EstadoReclamo estado) => estado switch
    {
        EstadoReclamo.Registrado => 1,
        EstadoReclamo.EnEvaluacion => 2,
        EstadoReclamo.EnProceso => 3,
        EstadoReclamo.Resuelto => 4,
        EstadoReclamo.Cerrado => 5,
        EstadoReclamo.Rechazado => 6,
        EstadoReclamo.Impugnado => 7,
        _ => throw new InvalidOperationException($"Estado {estado} sin equivalente en la base.")
    };
}
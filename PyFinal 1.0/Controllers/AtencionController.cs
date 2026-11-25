
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Models;
using System.Security.Claims;
using PyFinal_1.Domain.Enums;
using PyFinal_1.Domain.Estados;

namespace PyFinal_1.Controllers
{
    [Authorize(Roles = "Operario")]
    public class AtencionController : Controller
    {
        private const int EnProceso = 3;
        private const int Resuelto = 4;
        private const int Cerrado = 5;

        private readonly ReclamosDbContext _db;
        private readonly ILogger<AtencionController> _logger;

        public AtencionController(
            ReclamosDbContext db,
            ILogger<AtencionController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // MOSTRAR RECLAMOS DEL OPERARIO
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!ObtenerIdOperario(out int idOperario))
                return Forbid();

            var reclamos = await (
                from r in _db.Reclamos.AsNoTracking()
                join t in _db.TiposReclamo.AsNoTracking()
                    on r.IdTipoReclamo equals t.IdTipoReclamo
                join u in _db.Usuarios.AsNoTracking()
                    on r.IdUsuario equals u.IdUsuario
                where r.IdOperario == idOperario
                orderby r.IdEstado == EnProceso descending,
                        r.FechaRegistro descending
                select new AtencionReclamoVista
                {
                    Codigo = r.CodigoReclamo,
                    Tipo = t.NombreTipo,
                    Solicitante = u.NombresApellidos,
                    Descripcion = r.Detalle,
                    Fecha = r.FechaRegistro,
                    IdEstado = r.IdEstado
                }
            ).ToListAsync();

            return View(reclamos);
        }

        // GUARDAR AVANCE SIN RESOLVER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarAvance(
            string codigo,
            string? comentario)
        {
            return await ProcesarAtencion(
                codigo,
                comentario,
                resolver: false);
        }

        // MARCAR RECLAMO COMO RESUELTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolver(
            string codigo,
            string? comentario)
        {
            return await ProcesarAtencion(
                codigo,
                comentario,
                resolver: true);
        }
        // CERRAR RECLAMO RESUELTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(
            string codigo,
            string? comentario)
        {
            if (!ObtenerIdOperario(out int idOperario))
                return Forbid();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Error"] =
                    "Debes seleccionar un reclamo.";

                return RedirectToAction(nameof(Index));
            }

            comentario = comentario?.Trim() ?? "";

            if (comentario.Length > 220)
            {
                TempData["Error"] =
                    "El comentario no puede superar 220 caracteres.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                // BUSCAR RECLAMO ASIGNADO A ESTE OPERARIO
                var reclamo = await _db.Reclamos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.CodigoReclamo == codigo &&
                        r.IdOperario == idOperario);

                if (reclamo == null)
                {
                    TempData["Error"] =
                        "Este reclamo no está asignado a tu cuenta.";

                    return RedirectToAction(nameof(Index));
                }

                // PATRON STATE: EL ESTADO ACTUAL DECIDE SI SE PUEDE CERRAR
                EstadoReclamo estadoActual = reclamo.IdEstado switch
                {
                    1 => EstadoReclamo.Registrado,
                    2 => EstadoReclamo.EnEvaluacion,
                    3 => EstadoReclamo.EnProceso,
                    4 => EstadoReclamo.Resuelto,
                    5 => EstadoReclamo.Cerrado,
                    _ => EstadoReclamo.Desconocido
                };

                var contexto = new ReclamoContexto(estadoActual);
                var resultado = contexto.Cerrar();

                if (!resultado.Exito ||
                    contexto.EstadoActual != EstadoReclamo.Cerrado)
                {
                    TempData["Error"] = resultado.Mensaje;

                    return RedirectToAction(nameof(Index));
                }

                DateTime? fechaActual = DateTime.Now;

                // TRANSACCION PARA NO PERDER EL HISTORIAL
                await using var transaccion =
                    await _db.Database.BeginTransactionAsync();

                // ACTUALIZAR SOLO SI SIGUE ASIGNADO Y RESUELTO
                int actualizados = await _db.Reclamos
                    .Where(r =>
                        r.IdReclamo == reclamo.IdReclamo &&
                        r.IdOperario == idOperario &&
                        r.IdEstado == Resuelto)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.IdEstado, Cerrado)
                        .SetProperty(r => r.FechaActualizacion, fechaActual));

                if (actualizados != 1)
                {
                    TempData["Error"] =
                        "El reclamo cambió. Actualiza la página.";

                    return RedirectToAction(nameof(Index));
                }

                // GUARDAR EL CAMBIO EN EL HISTORIAL
                _db.HistorialReclamos.Add(
                    new HistorialReclamoBD
                    {
                        IdReclamo = reclamo.IdReclamo,
                        IdEstadoAnterior = Resuelto,
                        IdEstadoNuevo = Cerrado,
                        IdUsuarioModificacion = idOperario,
                        Comentario = string.IsNullOrWhiteSpace(comentario)
                            ? "Reclamo cerrado por el operario."
                            : "Reclamo cerrado. " + comentario,
                        FechaCambio = fechaActual
                    });

                await _db.SaveChangesAsync();

                await transaccion.CommitAsync();

                TempData["Ok"] =
                    $"El reclamo {codigo} fue cerrado correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error cerrando reclamo {Codigo}",
                    codigo);

                TempData["Error"] =
                    "No se pudo cerrar el reclamo. " +
                    "Revisa la base de datos e inténtalo nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // PROCESAR CAMBIO Y REGISTRAR HISTORIAL
        private async Task<IActionResult> ProcesarAtencion(
            string codigo,
            string? comentario,
            bool resolver)
        {
            if (!ObtenerIdOperario(out int idOperario))
                return Forbid();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Error"] =
                    "Debes seleccionar un reclamo.";

                return RedirectToAction(nameof(Index));
            }

            comentario = comentario?.Trim() ?? "";

            // VALIDAR OBSERVACION
            if (comentario.Length > 220)
            {
                TempData["Error"] =
                    "La observación no puede superar 220 caracteres.";

                return RedirectToAction(nameof(Index));
            }

            if (!resolver && comentario.Length < 10)
            {
                TempData["Error"] =
                    "Para guardar un avance debes escribir " +
                    "una observación de al menos 10 caracteres.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                // BUSCAR RECLAMO ASIGNADO
                var reclamo = await _db.Reclamos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.CodigoReclamo == codigo &&
                        r.IdOperario == idOperario);

                if (reclamo == null)
                {
                    TempData["Error"] =
                        "Este reclamo no está asignado a tu cuenta.";

                    return RedirectToAction(nameof(Index));
                }

                if (reclamo.IdEstado != EnProceso)
                {
                    TempData["Error"] =
                        "Solo se pueden actualizar reclamos En proceso.";

                    return RedirectToAction(nameof(Index));
                }
                int estadoNuevo = EnProceso;

                // PATRON STATE: EL ESTADO DECIDE SI SE PUEDE RESOLVER
                if (resolver)
                {
                    var contexto = new ReclamoContexto(EstadoReclamo.EnProceso);
                    var resultado = contexto.Resolver();

                    if (!resultado.Exito ||
                        contexto.EstadoActual != EstadoReclamo.Resuelto)
                    {
                        TempData["Error"] = resultado.Mensaje;

                        return RedirectToAction(nameof(Index));
                    }

                    estadoNuevo = Resuelto;
                }

                DateTime? fechaActual = DateTime.Now;

                // TRANSACCION PARA NO PERDER EL HISTORIAL
                await using var transaccion =
                    await _db.Database.BeginTransactionAsync();

                // ACTUALIZAR SOLO SI SIGUE ASIGNADO Y EN PROCESO
                int actualizados = await _db.Reclamos
                    .Where(r =>
                        r.IdReclamo == reclamo.IdReclamo &&
                        r.IdOperario == idOperario &&
                        r.IdEstado == EnProceso)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(
                            r => r.IdEstado,
                            estadoNuevo)
                        .SetProperty(
                            r => r.FechaActualizacion,
                            fechaActual));

                if (actualizados != 1)
                {
                    TempData["Error"] =
                        "El reclamo cambió. Actualiza la página.";

                    return RedirectToAction(nameof(Index));
                }

                string textoHistorial;

                if (resolver)
                {
                    textoHistorial = string.IsNullOrWhiteSpace(comentario)
                        ? "Reclamo resuelto por el operario."
                        : "Reclamo resuelto. " + comentario;
                }
                else
                {
                    textoHistorial =
                        "Avance registrado. " + comentario;
                }

                // GUARDAR EL CAMBIO EN EL HISTORIAL
                _db.HistorialReclamos.Add(
                    new HistorialReclamoBD
                    {
                        IdReclamo = reclamo.IdReclamo,
                        IdEstadoAnterior = EnProceso,
                        IdEstadoNuevo = estadoNuevo,
                        IdUsuarioModificacion = idOperario,
                        Comentario = textoHistorial,
                        FechaCambio = fechaActual
                    });

                await _db.SaveChangesAsync();

                await transaccion.CommitAsync();

                if (resolver)
                {
                    TempData["Ok"] =
                        $"El reclamo {codigo} fue marcado como Resuelto. " +
                        "Ahora puedes recibir otra asignación.";
                }
                else
                {
                    TempData["Ok"] =
                        $"Avance guardado para el reclamo {codigo}. " +
                        "El reclamo sigue En proceso.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error actualizando reclamo {Codigo}",
                    codigo);

                TempData["Error"] =
                    "No se pudo guardar el cambio. " +
                    "Revisa la base de datos e inténtalo nuevamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // OBTENER EL USUARIO AUTENTICADO
        private bool ObtenerIdOperario(out int idOperario)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out idOperario);
        }
    }

    // MODELO PARA LA PANTALLA DE ATENCION
    public class AtencionReclamoVista
    {
        public string Codigo { get; set; } = "";

        public string Tipo { get; set; } = "";

        public string Solicitante { get; set; } = "";

        public string Descripcion { get; set; } = "";

        public DateTime? Fecha { get; set; }

        public int IdEstado { get; set; }
    }
}
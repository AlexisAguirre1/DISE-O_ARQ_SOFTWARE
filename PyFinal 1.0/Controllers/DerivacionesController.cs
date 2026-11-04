
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Services;

namespace PyFinal_1.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class DerivacionesController : Controller
    {
        private readonly ReclamosDbContext _db;
        private readonly DerivacionService _servicio;

        private const int EnEvaluacion = 2;
        private const int EnProceso = 3;

        public DerivacionesController(
            ReclamosDbContext db,
            DerivacionService servicio)
        {
            _db = db;
            _servicio = servicio;
        }

        // RECLAMOS LISTOS PARA DERIVAR
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pendientes = await (
                from r in _db.Reclamos.AsNoTracking()
                join t in _db.TiposReclamo.AsNoTracking()
                    on r.IdTipoReclamo equals t.IdTipoReclamo
                join u in _db.Usuarios.AsNoTracking()
                    on r.IdUsuario equals u.IdUsuario
                where r.IdEstado == EnEvaluacion
                      && r.IdOperario == null
                orderby r.FechaRegistro descending
                select new DerivacionPendienteVista
                {
                    Codigo = r.CodigoReclamo,
                    Tipo = t.NombreTipo,
                    Solicitante = u.NombresApellidos,
                    Descripcion = r.Detalle,
                    Fecha = r.FechaRegistro,
                    IdEstado = r.IdEstado
                }
            ).ToListAsync();

            return View(pendientes);
        }

        // FORMULARIO DE ASIGNACION
        [HttpGet]
        public async Task<IActionResult> Derivar(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return RedirectToAction(nameof(Index));
            }

            var reclamo = await (
                from r in _db.Reclamos.AsNoTracking()
                join t in _db.TiposReclamo.AsNoTracking()
                    on r.IdTipoReclamo equals t.IdTipoReclamo
                join u in _db.Usuarios.AsNoTracking()
                    on r.IdUsuario equals u.IdUsuario
                where r.CodigoReclamo == codigo
                      && r.IdEstado == EnEvaluacion
                      && r.IdOperario == null
                select new DerivacionFormularioVista
                {
                    Codigo = r.CodigoReclamo,
                    Tipo = t.NombreTipo,
                    Solicitante = u.NombresApellidos,
                    Descripcion = r.Detalle,
                    Fecha = r.FechaRegistro,
                    IdTipoReclamo = r.IdTipoReclamo
                }
            ).FirstOrDefaultAsync();

            if (reclamo == null)
            {
                TempData["Error"] =
                    "El reclamo no está disponible para derivación.";

                return RedirectToAction(nameof(Index));
            }

            // SOLO MOSTRAR OPERARIOS LIBRES DEL MISMO TIPO
            reclamo.Operarios = await (
                from o in _db.Operarios.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking()
                    on o.IdUsuario equals u.IdUsuario
                where o.IdTipoReclamo == reclamo.IdTipoReclamo
                      && !_db.Reclamos.Any(r =>
                          r.IdOperario == o.IdUsuario
                          && r.IdEstado == EnProceso)
                orderby u.NombresApellidos
                select new OperarioOpcionVista
                {
                    Id = o.IdUsuario,
                    Nombre = u.NombresApellidos
                }
            ).ToListAsync();

            return View(reclamo);
        }

        // GUARDAR ASIGNACION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(
            string codigo,
            int operarioId)
        {
            if (string.IsNullOrWhiteSpace(codigo) ||
                operarioId <= 0)
            {
                TempData["Error"] =
                    "Debes seleccionar un operario válido.";

                return RedirectToAction(nameof(Index));
            }

            var reclamo = await _db.Reclamos
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.CodigoReclamo == codigo);

            if (reclamo == null ||
                reclamo.IdEstado != EnEvaluacion ||
                reclamo.IdOperario != null)
            {
                TempData["Error"] =
                    "El reclamo ya no está disponible para asignación.";

                return RedirectToAction(nameof(Index));
            }

            // VERIFICAR TIPO DE OPERARIO
            bool compatible = await _db.Operarios
                .AsNoTracking()
                .AnyAsync(o =>
                    o.IdUsuario == operarioId
                    && o.IdTipoReclamo == reclamo.IdTipoReclamo);

            if (!compatible)
            {
                TempData["Error"] =
                    "El operario no corresponde al tipo de reclamo.";

                return RedirectToAction(
                    nameof(Derivar),
                    new { codigo });
            }

            // COMPROBAR QUE NO ESTE OCUPADO
            bool ocupado = await _db.Reclamos
                .AsNoTracking()
                .AnyAsync(r =>
                    r.IdOperario == operarioId
                    && r.IdEstado == EnProceso);

            if (ocupado)
            {
                TempData["Error"] =
                    "Este operario ya está atendiendo otro reclamo.";

                return RedirectToAction(
                    nameof(Derivar),
                    new { codigo });
            }

            // UTILIZAR EL SERVICIO EXISTENTE
            var (exito, mensaje) =
                await _servicio.DerivarAsync(
                    codigo, operarioId);

            if (exito)
            {
                TempData["Ok"] = mensaje;

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = mensaje;

            return RedirectToAction(
                nameof(Derivar),
                new { codigo });
        }
    }

    // MODELO PARA LA TABLA
    public class DerivacionPendienteVista
    {
        public string Codigo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Solicitante { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public DateTime? Fecha { get; set; }
        public int IdEstado { get; set; }
    }

    // MODELO PARA EL FORMULARIO
    public class DerivacionFormularioVista
    {
        public string Codigo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Solicitante { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public DateTime? Fecha { get; set; }
        public int IdTipoReclamo { get; set; }

        public List<OperarioOpcionVista> Operarios
        {
            get;
            set;
        } = new();
    }

    // MODELO DE OPERARIOS
    public class OperarioOpcionVista
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
    }
}

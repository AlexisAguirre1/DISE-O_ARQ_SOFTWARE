
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Models;
using PyFinal_1.ViewModels.Reclamos;
using PyFinal_1.Domain.Enums;
using PyFinal_1.Domain.Estados;
using PyFinal_1._0.Domain;
using System.Security.Claims;

namespace PyFinal_1.Controllers
{
    [Authorize(Roles = "Solicitante,Supervisor,Operario")]
    public class ReclamosController : Controller
    {
        private readonly ReclamosDbContext _db;
        private readonly ILogger<ReclamosController> _logger;

        private readonly ConfiguracionSistema configuracion =
            ConfiguracionSistema.Instancia;

        public ReclamosController(
            ReclamosDbContext db,
            ILogger<ReclamosController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // MOSTRAR RECLAMOS SEGUN EL ROL
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idUsuario))
            {
                return Forbid();
            }

            var consulta = _db.Reclamos.AsNoTracking();

            if (User.IsInRole("Solicitante"))
            {
                consulta = consulta.Where(
                    r => r.IdUsuario == idUsuario);
            }
            else if (User.IsInRole("Operario"))
            {
                consulta = consulta.Where(
                    r => r.IdOperario == idUsuario);
            }
            else if (!User.IsInRole("Supervisor"))
            {
                return Forbid();
            }

            var datos = await (
                from r in consulta
                join t in _db.TiposReclamo.AsNoTracking()
                    on r.IdTipoReclamo equals t.IdTipoReclamo
                orderby r.IdReclamo descending
                select new
                {
                    r.CodigoReclamo,
                    t.NombreTipo,
                    r.Detalle,
                    r.FechaRegistro,
                    r.IdEstado
                }
            ).ToListAsync();

            var reclamos = datos.Select(r => new ListaReclamoVM
            {
                Codigo = r.CodigoReclamo,
                TipoReclamo = r.NombreTipo,
                Descripcion = r.Detalle,
                FechaRegistro = r.FechaRegistro
                    ?? DateTime.MinValue,

                Estado = r.IdEstado switch
                {
                    1 => EstadoReclamo.Registrado,
                    2 => EstadoReclamo.EnEvaluacion,
                    3 => EstadoReclamo.EnProceso,
                    4 => EstadoReclamo.Resuelto,
                    5 => EstadoReclamo.Cerrado,
                    _ => EstadoReclamo.Desconocido
                }
            }).ToList();

            return View(reclamos);
        }

        // FORMULARIO DE REGISTRO: SOLO SOLICITANTE
        [HttpGet]
        [Authorize(Roles = "Solicitante")]
        public IActionResult Create()
        {
            return View(new CrearReclamoVM());
        }

        // GUARDAR RECLAMO EN SQL SERVER
        [HttpPost]
        [Authorize(Roles = "Solicitante")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearReclamoVM vm)
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idUsuario))
            {
                return Forbid();
            }

            // VALIDAR TIPO DE RECLAMO
            if (string.IsNullOrWhiteSpace(vm.TipoReclamo) ||
                !configuracion.EsTipoPermitido(vm.TipoReclamo))
            {
                ModelState.AddModelError(
                    nameof(vm.TipoReclamo),
                    "Tipo de reclamo no válido.");
            }

            // VALIDAR DNI
            if (string.IsNullOrWhiteSpace(vm.Dni) ||
                vm.Dni.Length != 8 ||
                !vm.Dni.All(char.IsAsciiDigit))
            {
                ModelState.AddModelError(
                    nameof(vm.Dni),
                    "El DNI debe tener 8 números.");
            }

            // VALIDAR DESCRIPCION
            if (string.IsNullOrWhiteSpace(vm.Descripcion) ||
                vm.Descripcion.Trim().Length < 20 ||
                vm.Descripcion.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(vm.Descripcion),
                    "La descripción debe tener entre 20 y 500 caracteres.");
            }

            // VALIDAR EVIDENCIA
            if (vm.Evidencia != null)
            {
                var ext = Path.GetExtension(
                    vm.Evidencia.FileName).ToLowerInvariant();

                if (!configuracion.EsExtensionPermitida(ext))
                {
                    ModelState.AddModelError(
                        nameof(vm.Evidencia),
                        "Use JPG, PNG o PDF.");
                }
                else if (vm.Evidencia.Length >
                         configuracion.TamanoMaximoBytes)
                {
                    ModelState.AddModelError(
                        nameof(vm.Evidencia),
                        "El archivo supera el tamaño permitido.");
                }
                else
                {
                    ModelState.AddModelError(
                        nameof(vm.Evidencia),
                        "La carga de evidencias aún no está disponible.");
                }
            }

            if (!ModelState.IsValid)
                return View(vm);

            // OBTENER USUARIO AUTENTICADO
            var usuario = await _db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.IdUsuario == idUsuario);

            if (usuario == null)
                return Forbid();

            // VALIDAR NOMBRE Y CORREO
            bool mismoNombre = string.Equals(
                usuario.NombresApellidos.Trim(),
                vm.NombreCompleto.Trim(),
                StringComparison.OrdinalIgnoreCase);

            bool mismoCorreo = string.Equals(
                usuario.Correo.Trim(),
                vm.Correo.Trim(),
                StringComparison.OrdinalIgnoreCase);

            if (!mismoNombre || !mismoCorreo)
            {
                ModelState.AddModelError(
                    "",
                    "El nombre y correo deben coincidir con tu cuenta.");

                return View(vm);
            }

            // VALIDAR DNI DE LA CUENTA
            if (string.IsNullOrEmpty(usuario.DNI) ||
                usuario.DNI != vm.Dni)
            {
                ModelState.AddModelError(
                    nameof(vm.Dni),
                    "El DNI no coincide con el registrado en tu cuenta.");

                return View(vm);
            }

            // OBTENER TIPO DE RECLAMO
            var tipo = await _db.TiposReclamo
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.NombreTipo == vm.TipoReclamo);

            if (tipo == null)
            {
                ModelState.AddModelError(
                    nameof(vm.TipoReclamo),
                    "Tipo de reclamo no encontrado.");

                return View(vm);
            }

            // OBTENER ESTADO REGISTRADO
            int idEstado = await _db.Database.SqlQuery<int>(
                $"""
                SELECT [IdEstado] AS [Value]
                FROM [dbo].[EstadosReclamo]
                WHERE [NombreEstado] = {"Registrado"}
                """
            ).FirstOrDefaultAsync();

            if (idEstado == 0)
            {
                ModelState.AddModelError(
                    "",
                    "No se encontró el estado Registrado.");

                return View(vm);
            }

            // CREAR RECLAMO
            string codigo = "REC-" +
                Guid.NewGuid().ToString("N")[..16];

            var nuevoReclamo = new ReclamoBD
            {
                CodigoReclamo = codigo,
                IdUsuario = idUsuario,
                IdTipoReclamo = tipo.IdTipoReclamo,
                IdEstado = idEstado,
                IdOperario = null,
                Detalle = vm.Descripcion.Trim(),
                FechaRegistro = DateTime.Now
            };

            try
            {
                _db.Reclamos.Add(nuevoReclamo);
                await _db.SaveChangesAsync();

                TempData["Ok"] =
                    $"Reclamo registrado correctamente. Código: {codigo}";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex, "Error guardando reclamo");

                ModelState.AddModelError(
                    "",
                    "No se pudo guardar el reclamo.");

                return View(vm);
            }
        }

        // EVALUAR RECLAMO: SOLO SUPERVISOR
        [HttpPost]
        [Authorize(Roles = "Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evaluar(string codigo)
        {
            // IDENTIFICAR AL SUPERVISOR
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idSupervisor))
            {
                return Forbid();
            }

            bool esSupervisor = await _db.Usuarios
                .AsNoTracking()
                .AnyAsync(u =>
                    u.IdUsuario == idSupervisor &&
                    u.IdRol == 2);

            if (!esSupervisor)
                return Forbid();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Error"] =
                    "El código del reclamo es inválido.";

                return RedirectToAction(nameof(Index));
            }

            // BUSCAR RECLAMO EN SQL SERVER
            var reclamo = await _db.Reclamos
                .FirstOrDefaultAsync(
                    r => r.CodigoReclamo == codigo);

            if (reclamo == null)
            {
                TempData["Error"] =
                    "El reclamo no existe.";

                return RedirectToAction(nameof(Index));
            }

            // SOLO EVALUAR RECLAMOS REGISTRADOS
            if (reclamo.IdEstado != 1 ||
                reclamo.IdOperario != null)
            {
                TempData["Error"] =
                    "Solo se pueden evaluar reclamos registrados.";

                return RedirectToAction(nameof(Index));
            }

            // VALIDAR TRANSICION CON PATRON STATE
            var contexto = new ReclamoContexto(
                EstadoReclamo.Registrado);

            var resultado = contexto.Evaluar();

            if (!resultado.Exito ||
                contexto.EstadoActual != EstadoReclamo.EnEvaluacion)
            {
                TempData["Error"] = resultado.Mensaje;

                return RedirectToAction(nameof(Index));
            }

            // ACTUALIZAR RECLAMO
            reclamo.IdEstado = 2;
            reclamo.FechaActualizacion = DateTime.Now;

            // GUARDAR HISTORIAL DE EVALUACION
            _db.HistorialReclamos.Add(
                new HistorialReclamoBD
                {
                    IdReclamo = reclamo.IdReclamo,
                    IdEstadoAnterior = 1,
                    IdEstadoNuevo = 2,
                    IdUsuarioModificacion = idSupervisor,
                    Comentario =
                        "Reclamo evaluado por el supervisor.",
                    FechaCambio = DateTime.Now
                });

            try
            {
                await _db.SaveChangesAsync();

                TempData["Ok"] =
                    "Reclamo evaluado correctamente. " +
                    "Ahora puedes derivarlo a un operario.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex, "Error evaluando reclamo");

                TempData["Error"] =
                    "No se pudo guardar la evaluación.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

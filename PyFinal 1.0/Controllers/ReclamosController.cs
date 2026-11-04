
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Models;
using PyFinal_1.ViewModels.Reclamos;
using PyFinal_1.Domain.Enums;
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

        // MOSTRAR RECLAMOS SEGUN ROL
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

        // FORMULARIO: SOLO SOLICITANTE
        [HttpGet]
        [Authorize(Roles = "Solicitante")]
        public IActionResult Create()
        {
            return View(new CrearReclamoVM());
        }

        // GUARDAR RECLAMO
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

            // VALIDAR TIPO
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

            // VALIDAR DATOS CONTRA LA CUENTA
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

            if (string.IsNullOrEmpty(usuario.DNI) ||
                usuario.DNI != vm.Dni)
            {
                ModelState.AddModelError(
                    nameof(vm.Dni),
                    "El DNI no coincide con el registrado en tu cuenta.");

                return View(vm);
            }

            // BUSCAR TIPO
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

            // BUSCAR ESTADO REGISTRADO
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

            // GUARDAR EN SQL SERVER
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
    }
}

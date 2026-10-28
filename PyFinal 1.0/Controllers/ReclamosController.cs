using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Models;
using PyFinal_1.ViewModels.Reclamos;
using PyFinal_1.Domain.Enums;
using PyFinal_1._0.Domain;

namespace PyFinal_1.Controllers
{
    public class ReclamosController : Controller
    {
        private readonly ReclamosDbContext _db;

        // PATRÓN SINGLETON
        private readonly ConfiguracionSistema configuracion =
            ConfiguracionSistema.Instancia;

        // Constructor de conexión con SQL Server
        public ReclamosController(ReclamosDbContext db)
        {
            _db = db;
        }

        // MOSTRAR RECLAMOS DESDE SQL SERVER
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var datos = await (
                from r in _db.Reclamos.AsNoTracking()
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

            // Convertir los estados de SQL Server
            var reclamos = datos.Select(r => new ListaReclamoVM
            {
                Codigo = r.CodigoReclamo,
                TipoReclamo = r.NombreTipo,
                Descripcion = r.Detalle,
                FechaRegistro = r.FechaRegistro ?? DateTime.MinValue,
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

        // MOSTRAR FORMULARIO
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CrearReclamoVM());
        }

        // REGISTRAR RECLAMO EN SQL SERVER
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearReclamoVM vm)
        {
            // SINGLETON: Validar tipo de reclamo
            if (!configuracion.EsTipoPermitido(vm.TipoReclamo))
            {
                ModelState.AddModelError(
                    nameof(vm.TipoReclamo),
                    "Tipo de reclamo no válido."
                );
            }

            // SINGLETON: Validar archivos
            if (vm.Evidencia is not null)
            {
                var ext = Path.GetExtension(
                    vm.Evidencia.FileName
                ).ToLowerInvariant();

                if (!configuracion.EsExtensionPermitida(ext))
                {
                    ModelState.AddModelError(
                        nameof(vm.Evidencia),
                        "Formato no permitido. Use JPG, PNG o PDF."
                    );
                }
                else if (vm.Evidencia.Length >
                         configuracion.TamanoMaximoBytes)
                {
                    ModelState.AddModelError(
                        nameof(vm.Evidencia),
                        "El archivo supera los 5 MB."
                    );
                }
                else
                {
                    ModelState.AddModelError(
                        nameof(vm.Evidencia),
                        "Aún no se pueden almacenar evidencias. Registre sin archivo."
                    );
                }
            }

            // Validar longitud admitida por SQL Server
            if (vm.Descripcion?.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(vm.Descripcion),
                    "El detalle no puede superar 500 caracteres."
                );
            }

            if (!ModelState.IsValid)
                return View(vm);

            // SQL SERVER: Buscar usuario existente
            var usuarios = await _db.Database.SqlQuery<int>(
                $"""
                SELECT [IdUsuario] AS [Value]
                FROM [dbo].[Usuarios]
                WHERE [Correo] = {vm.Correo.Trim()}
                AND [NombresApellidos] = {vm.NombreCompleto.Trim()}
                """
            ).Take(2).ToListAsync();

            if (usuarios.Count != 1)
            {
                ModelState.AddModelError(
                    nameof(vm.Correo),
                    "Para esta prueba, ingrese los datos de un usuario existente."
                );

                return View(vm);
            }

            // SQL SERVER: Buscar tipo de reclamo
            var tipo = await _db.TiposReclamo
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.NombreTipo == vm.TipoReclamo
                );

            if (tipo == null)
            {
                ModelState.AddModelError(
                    nameof(vm.TipoReclamo),
                    "El tipo de reclamo no existe en SQL Server."
                );

                return View(vm);
            }

            // SQL SERVER: Obtener estado Registrado
            var estado = "Registrado";

            var idEstado = await _db.Database.SqlQuery<int>(
                $"""
                SELECT [IdEstado] AS [Value]
                FROM [dbo].[EstadosReclamo]
                WHERE [NombreEstado] = {estado}
                """
            ).FirstOrDefaultAsync();

            if (idEstado == 0)
            {
                ModelState.AddModelError(
                    "",
                    "No se encontró el estado Registrado."
                );

                return View(vm);
            }

            // Generar código de reclamo
            var codigo = "REC-" +
                Guid.NewGuid().ToString("N")[..16];

            // Crear objeto para SQL Server
            var nuevoReclamo = new ReclamoBD
            {
                CodigoReclamo = codigo,
                IdUsuario = usuarios[0],
                IdTipoReclamo = tipo.IdTipoReclamo,
                IdEstado = idEstado,
                Detalle = (vm.Descripcion ?? string.Empty).Trim(),
                FechaRegistro = DateTime.Now
            };

            // GUARDAR EN SQL SERVER
            _db.Reclamos.Add(nuevoReclamo);
            await _db.SaveChangesAsync();

            // Confirmación
            TempData["Ok"] =
                $"Reclamo registrado correctamente. Código: {codigo}";

            return RedirectToAction(nameof(Index));
        }
    }
}
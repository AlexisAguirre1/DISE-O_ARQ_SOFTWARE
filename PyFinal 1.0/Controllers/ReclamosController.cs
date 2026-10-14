using Microsoft.AspNetCore.Mvc;
using PyFinal_1.ViewModels.Reclamos;
using PyFinal_1.Domain.Enums;


namespace PyFinal_1.Controllers
{
    public class ReclamosController : Controller
    {
        private static readonly string[] TiposPermitidos =
            { "Facturacion", "Cobertura", "Atencion", "Equipos" };

        private static readonly string[] ExtensionesPermitidas =
            { ".jpg", ".jpeg", ".png", ".pdf" };

        private const long TamanoMaximoBytes = 5 * 1024 * 1024;

        [HttpGet]
        public IActionResult Index()
        {
            var reclamos = new List<ListaReclamoVM>
            {
              new() { Codigo = "REC-20260924-4821", TipoReclamo = "Facturacion", Descripcion = "Cobro duplicado en el recibo de septiembre.", FechaRegistro = DateTime.Now.AddDays(-5), Estado = EstadoReclamo.EnEvaluacion },
              new() { Codigo = "REC-20260918-1032", TipoReclamo = "Cobertura", Descripcion = "Sin señal en la zona desde hace una semana.", FechaRegistro = DateTime.Now.AddDays(-11), Estado = EstadoReclamo.Resuelto },
              new() { Codigo = "REC-20260910-7765", TipoReclamo = "Atencion", Descripcion = "Mala atención en tienda física de San Isidro.", FechaRegistro = DateTime.Now.AddDays(-19), Estado = EstadoReclamo.Rechazado }
            };

            return View(reclamos);
        }

        [HttpGet]
        public IActionResult Create() => View(new CrearReclamoVM());

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(CrearReclamoVM vm)
        {
            if (!TiposPermitidos.Contains(vm.TipoReclamo))
                ModelState.AddModelError(nameof(vm.TipoReclamo), "Tipo de reclamo no válido.");

            if (vm.Evidencia is not null)
            {
                var ext = Path.GetExtension(vm.Evidencia.FileName).ToLowerInvariant();
                if (!ExtensionesPermitidas.Contains(ext))
                    ModelState.AddModelError(nameof(vm.Evidencia), "Formato no permitido. Use JPG, PNG o PDF.");
                else if (vm.Evidencia.Length > TamanoMaximoBytes)
                    ModelState.AddModelError(nameof(vm.Evidencia), "El archivo supera el tamaño máximo de 5 MB.");
            }

            if (!ModelState.IsValid) return View(vm);

            var codigo = $"REC-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            TempData["Ok"] = $"Reclamo registrado correctamente. Código: {codigo}";
            return RedirectToAction(nameof(Index));
        }
    }
}

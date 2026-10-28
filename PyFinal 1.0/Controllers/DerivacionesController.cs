using Microsoft.AspNetCore.Mvc;
using PyFinal_1.Services;

namespace PyFinal_1.Controllers;

public class DerivacionesController : Controller
{
    private readonly DerivacionService _servicio;

    public DerivacionesController(DerivacionService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pendientes = await _servicio.ObtenerPendientesAsync();
        return View(pendientes);
    }

    [HttpGet]
    public async Task<IActionResult> Derivar(string codigo)
    {
        var vm = await _servicio.ObtenerParaDerivarAsync(codigo);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Recibe el clic del botón y guarda la derivación
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(string codigo, int operarioId)
    {
        var (exito, mensaje) = await _servicio.DerivarAsync(codigo, operarioId);

        if (exito)
        {
            TempData["Ok"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = mensaje;
        return RedirectToAction(nameof(Derivar), new { codigo });
    }
}
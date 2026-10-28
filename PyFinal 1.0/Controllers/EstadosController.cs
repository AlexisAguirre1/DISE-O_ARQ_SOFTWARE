using Microsoft.AspNetCore.Mvc;
using PyFinal_1.Domain.Estados;
using PyFinal_1.ViewModels.Estados;

namespace PyFinal_1.Controllers;

public class EstadosController : Controller
{
    [HttpGet]
    public IActionResult Demo() => View(new DemoEstadoVM());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Demo(DemoEstadoVM vm)
    {
        
        var contexto = new ReclamoContexto(vm.EstadoInicial);

        var resultado = vm.Accion switch
        {
            "Evaluar" => contexto.Evaluar(),
            "Derivar" => contexto.Derivar(vm.HayOperarioLibre),
            "Resolver" => contexto.Resolver(),
            "Cerrar" => contexto.Cerrar(),
            _ => ResultadoTransicion.Rechazada("Acción no válida.")
        };

        vm.Ejecutado = true;
        vm.Exito = resultado.Exito;
        vm.Mensaje = resultado.Mensaje;
        vm.EstadoFinal = contexto.EstadoActual;

        return View(vm);
    }
}
using Microsoft.AspNetCore.Mvc;

namespace PyFinal_1.Controllers
{
    public class AccountController : Controller
    {
        // Mostrar pantalla de inicio de sesión
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Recibir datos del formulario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(
            string correo,
            string contrasena)
        {
            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasena))
            {
                ViewBag.Error =
                    "Ingrese su correo y contraseña.";
            }
            else
            {
                ViewBag.Error =
                    "Pantalla lista. Falta configurar la autenticación segura.";
            }

            return View();
        }
    }
}
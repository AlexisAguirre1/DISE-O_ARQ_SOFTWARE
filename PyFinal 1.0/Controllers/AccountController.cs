
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using PyFinal_1.ViewModels.Cuentas;

namespace PyFinal_1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ReclamosDbContext _db;
        private readonly ILogger<AccountController> _logger;

        private readonly PasswordHasher<UsuarioBD> _hasher = new();

        public AccountController(
            ReclamosDbContext db,
            ILogger<AccountController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // MOSTRAR LOGIN
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Reclamos");

            return View();
        }

        // INICIAR SESION
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string? correo,
            string? contrasena,
            bool recordarme = false)
        {
            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasena))
            {
                ViewBag.Error = "Ingrese su correo y contraseña.";
                return View();
            }

            try
            {
                correo = correo.Trim();

                var usuarios = await _db.Usuarios
                    .Where(u => u.Correo == correo)
                    .Take(2)
                    .ToListAsync();

                if (usuarios.Count != 1)
                {
                    ViewBag.Error = "Correo o contraseña incorrectos.";
                    return View();
                }

                var usuario = usuarios[0];

                PasswordVerificationResult verificacion;

                try
                {
                    verificacion = _hasher.VerifyHashedPassword(
                        usuario,
                        usuario.Contrasena,
                        contrasena);
                }
                catch (FormatException)
                {
                    verificacion = PasswordVerificationResult.Failed;
                }

                // COMPATIBILIDAD CON CONTRASEÑAS ANTIGUAS
                bool antigua = false;

                if (verificacion == PasswordVerificationResult.Failed &&
                    !EsHashIdentity(usuario.Contrasena))
                {
                    var a = SHA256.HashData(
                        Encoding.UTF8.GetBytes(usuario.Contrasena));

                    var b = SHA256.HashData(
                        Encoding.UTF8.GetBytes(contrasena));

                    antigua = CryptographicOperations
                        .FixedTimeEquals(a, b);
                }

                if (verificacion == PasswordVerificationResult.Failed &&
                    !antigua)
                {
                    ViewBag.Error = "Correo o contraseña incorrectos.";
                    return View();
                }

                // OBTENER ROL DESDE SQL SERVER
                var nombreRol = await _db.Database.SqlQuery<string>(
                    $"""
                    SELECT [NombreRol] AS [Value]
                    FROM [dbo].[Roles]
                    WHERE [IdRol] = {usuario.IdRol}
                    """
                ).FirstOrDefaultAsync();

                string? rol = new[]
                {
                    "Solicitante", "Supervisor", "Operario"
                }.FirstOrDefault(r =>
                    string.Equals(
                        r,
                        nombreRol?.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (rol == null)
                {
                    ViewBag.Error = "La cuenta no tiene un rol válido.";
                    return View();
                }

                // PROTEGER CONTRASEÑAS ANTIGUAS
                if (antigua ||
                    verificacion ==
                    PasswordVerificationResult.SuccessRehashNeeded)
                {
                    usuario.Contrasena = _hasher.HashPassword(
                        usuario, contrasena);

                    await _db.SaveChangesAsync();
                }

                // DATOS DEL USUARIO AUTENTICADO
                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        usuario.IdUsuario.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        usuario.NombresApellidos),

                    new Claim(
                        ClaimTypes.Email,
                        usuario.Correo),

                    new Claim(
                        ClaimTypes.Role,
                        rol)
                };

                var identidad = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identidad);

                var propiedades = new AuthenticationProperties
                {
                    IsPersistent = recordarme,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(
                        recordarme
                            ? TimeSpan.FromDays(7)
                            : TimeSpan.FromHours(8))
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    propiedades);

                _logger.LogInformation(
                    "Sesión iniciada por el usuario {Id}",
                    usuario.IdUsuario);

                return RedirectToAction("Index", "Reclamos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el inicio de sesión.");
                ViewBag.Error = "No se pudo iniciar sesión. Inténtalo nuevamente.";
                return View();
            }
        }

        // DETECTAR HASH DE ASP.NET IDENTITY
        private static bool EsHashIdentity(string valor)
        {
            try
            {
                byte[] datos = Convert.FromBase64String(valor);

                return datos.Length >= 49 &&
                    (datos[0] == 0x00 || datos[0] == 0x01);
            }
            catch (FormatException)
            {
                return false;
            }
        }
        // MOSTRAR REGISTRO DE CUENTA
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Reclamos");

            return View(new RegistrarCuentaVM());
        }

        // CREAR CUENTA (el registro público siempre crea un Solicitante)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrarCuentaVM vm)
        {
            vm.NombresApellidos = (vm.NombresApellidos ?? "").Trim();
            vm.Correo = (vm.Correo ?? "").Trim();
            vm.DNI = (vm.DNI ?? "").Trim();

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                // Correo y DNI no pueden repetirse
                if (await _db.Usuarios.AnyAsync(u => u.Correo == vm.Correo))
                    ModelState.AddModelError(nameof(vm.Correo),
                        "Este correo ya está registrado.");

                if (await _db.Usuarios.AnyAsync(u => u.DNI == vm.DNI))
                    ModelState.AddModelError(nameof(vm.DNI),
                        "Este DNI ya está registrado.");

                if (!ModelState.IsValid)
                    return View(vm);

                // El rol lo decide el sistema, nunca el formulario
                string nombreRol = "Solicitante";

                int idRol = await _db.Database.SqlQuery<int>(
                    $"SELECT [IdRol] AS [Value] FROM [dbo].[Roles] WHERE [NombreRol] = {nombreRol}")
                    .FirstOrDefaultAsync();

                if (idRol == 0)
                {
                    ViewBag.Error =
                        "No se pudo crear la cuenta: falta configurar los roles del sistema.";
                    return View(vm);
                }

                var usuario = new UsuarioBD
                {
                    NombresApellidos = vm.NombresApellidos,
                    Correo = vm.Correo,
                    DNI = vm.DNI,
                    IdRol = idRol
                };

                // La contraseña se guarda cifrada, igual que en el login
                usuario.Contrasena = _hasher.HashPassword(usuario, vm.Contrasena);

                _db.Usuarios.Add(usuario);
                await _db.SaveChangesAsync();

                TempData["Ok"] =
                    "Cuenta creada correctamente. Ya puedes iniciar sesión.";

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la cuenta.");
                ViewBag.Error =
                    "No se pudo crear la cuenta. Inténtalo nuevamente.";
                return View(vm);
            }
        }

        // CERRAR SESION
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        // ACCESO DENEGADO
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            Response.StatusCode = 403;

            return Content(
                "Acceso denegado. No tienes permisos para esta sección.",
                "text/plain; charset=utf-8");
        }
    }
}

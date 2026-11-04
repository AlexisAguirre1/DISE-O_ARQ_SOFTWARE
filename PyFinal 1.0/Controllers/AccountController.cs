
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

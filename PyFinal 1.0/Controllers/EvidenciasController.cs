using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Services;
using System.Security.Claims;

namespace PyFinal_1.Controllers
{
    [Authorize(Roles = "Solicitante,Supervisor,Operario")]
    public class EvidenciasController : Controller
    {
        private readonly ReclamosDbContext _db;
        private readonly EvidenciaService _evidencias;

        public EvidenciasController(
            ReclamosDbContext db,
            EvidenciaService evidencias)
        {
            _db = db;
            _evidencias = evidencias;
        }

        [HttpGet]
        public async Task<IActionResult> Descargar(int id)
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idUsuario))
            {
                return Forbid();
            }

            var datos = await (
                from e in _db.Evidencias.AsNoTracking()
                join r in _db.Reclamos.AsNoTracking()
                    on e.IdReclamo equals r.IdReclamo
                where e.IdEvidencia == id
                select new
                {
                    e.NombreArchivo,
                    e.NombreOriginal,
                    e.TipoContenido,
                    r.IdUsuario,
                    r.IdOperario
                }
            ).FirstOrDefaultAsync();

            if (datos == null)
                return NotFound();

            // QUIEN PUEDE VER LA EVIDENCIA
            bool permitido =
                User.IsInRole("Supervisor") ||
                (User.IsInRole("Solicitante") && datos.IdUsuario == idUsuario) ||
                (User.IsInRole("Operario") && datos.IdOperario == idUsuario);

            if (!permitido)
                return Forbid();

            string ruta = _evidencias.RutaFisica(datos.NombreArchivo);

            if (!System.IO.File.Exists(ruta))
                return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Content-Disposition"] =
                "inline; filename*=UTF-8''" +
                Uri.EscapeDataString(datos.NombreOriginal);

            // Se abre en el navegador (imagen o PDF)
            return PhysicalFile(ruta, datos.TipoContenido);
        }
    }
}
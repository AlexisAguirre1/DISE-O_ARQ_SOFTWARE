
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Domain.Enums;
using PyFinal_1.ViewModels.Reclamos;
using System.Security.Claims;

namespace PyFinal_1.Controllers
{
    [Authorize(Roles = "Solicitante,Supervisor")]
    public class SeguimientoController : Controller
    {
        private readonly ReclamosDbContext _db;

        public SeguimientoController(ReclamosDbContext db)
        {
            _db = db;
        }

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

            // SOLICITANTE: SOLO SUS RECLAMOS
            if (User.IsInRole("Solicitante"))
            {
                consulta = consulta.Where(
                    r => r.IdUsuario == idUsuario);
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

            var resultado = datos.Select(r => new ListaReclamoVM
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

            return View(resultado);
        }
    }
}


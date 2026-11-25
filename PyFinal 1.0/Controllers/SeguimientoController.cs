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

            bool esSolicitante = User.IsInRole("Solicitante");

            // SOLICITANTE: SOLO SUS RECLAMOS
            if (esSolicitante)
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
                join s in _db.Usuarios.AsNoTracking()
                    on r.IdUsuario equals s.IdUsuario
                orderby r.IdReclamo descending
                select new
                {
                    r.IdReclamo,
                    r.CodigoReclamo,
                    t.NombreTipo,
                    r.Detalle,
                    r.FechaRegistro,
                    r.IdEstado,
                    Solicitante = s.NombresApellidos
                }
            ).ToListAsync();

            // HISTORIAL DE LOS RECLAMOS VISIBLES
            var ids = datos.Select(d => d.IdReclamo).ToList();

            var cambios = await (
                from h in _db.HistorialReclamos.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking()
                    on h.IdUsuarioModificacion equals u.IdUsuario
                where ids.Contains(h.IdReclamo)
                orderby h.FechaCambio, h.IdHistorial
                select new
                {
                    h.IdReclamo,
                    h.IdEstadoAnterior,
                    h.IdEstadoNuevo,
                    h.Comentario,
                    h.FechaCambio,
                    IdAutor = u.IdUsuario,
                    Autor = u.NombresApellidos,
                    u.IdRol
                }
            ).ToListAsync();

            var cambiosPorReclamo = cambios.ToLookup(c => c.IdReclamo);

            var resultado = datos.Select(r =>
            {
                // El registro inicial no se guarda en HistorialReclamos,
                // por eso se agrega aquí como primer evento
                var historial = new List<HistorialItemVM>
                {
                    new()
                    {
                        Fecha = FormatearFecha(r.FechaRegistro),
                        Titulo = "Registrado",
                        Detalle = "El reclamo fue registrado en el sistema.",
                        Autor = esSolicitante ?  string.Empty : r.Solicitante
                    }
                };

                historial.AddRange(
                    cambiosPorReclamo[r.IdReclamo].Select(c => new HistorialItemVM
                    {
                        Fecha = FormatearFecha(c.FechaCambio),
                        Titulo = NombreEstado(c.IdEstadoAnterior)
                                 + " → "
                                 + NombreEstado(c.IdEstadoNuevo),
                        Detalle = c.Comentario ?? string.Empty,
                        Autor = NombreAutor(
                            c.IdAutor, c.Autor, c.IdRol,
                            idUsuario, esSolicitante)
                    }));

                return new ListaReclamoVM
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
                        6 => EstadoReclamo.Rechazado,
                        7 => EstadoReclamo.Impugnado,
                        _ => EstadoReclamo.Desconocido
                    },

                    Historial = historial
                };
            }).ToList();

            return View(resultado);
        }

        private static string FormatearFecha(DateTime? fecha)
            => fecha.HasValue
                ? fecha.Value.ToString("dd/MM/yyyy HH:mm")
                : "Sin fecha";

        private static string NombreEstado(int idEstado) => idEstado switch
        {
            1 => "Registrado",
            2 => "En evaluación",
            3 => "En proceso",
            4 => "Resuelto",
            5 => "Cerrado",
            6 => "Rechazado",
            7 => "Impugnado",
            _ => "Desconocido"
        };

        // El solicitante ve solo el rol del personal;
        // el supervisor ve el nombre completo
        // El solicitante no ve quién hizo cada cambio (solo qué pasó y cuándo);
        // el supervisor ve nombre y rol, para dejar trazabilidad
        private static string NombreAutor(
            int idAutor,
            string nombre,
            int idRol,
            int idActual,
            bool esSolicitante)
        {
            if (esSolicitante)
                return string.Empty;
                
            string rol = idRol switch
            {
                1 => "Solicitante",
                2 => "Supervisor",
                3 => "Operario",
                _ => "Usuario"
            };

            return $"{nombre} ({rol})";
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.Models;
using PyFinal_1.ViewModels.Reportes;

namespace PyFinal_1.Controllers
{
    // SOLO EL SUPERVISOR PUEDE VER LOS REPORTES
    [Authorize(Roles = "Supervisor")]
    public class ReportesController : Controller
    {
        private const int EnProceso = 3;
        private const int Resuelto = 4;
        private const int Cerrado = 5;

        private static readonly (int Id, string Nombre)[] Estados =
        {
            (1, "Registrado"),
            (2, "En evaluación"),
            (3, "En proceso"),
            (4, "Resuelto"),
            (5, "Cerrado"),
            (6, "Rechazado"),
            (7, "Impugnado")
        };

        private readonly ReclamosDbContext _db;

        public ReportesController(ReclamosDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue &&
                desde.Value > hasta.Value)
            {
                TempData["Error"] =
                    "La fecha inicial no puede ser mayor que la final.";

                return RedirectToAction(nameof(Index));
            }

            // RECLAMOS DENTRO DEL RANGO DE FECHAS (SI SE INDICO)
            IQueryable<ReclamoBD> consulta = _db.Reclamos.AsNoTracking();

            if (desde.HasValue)
            {
                DateTime inicio = desde.Value.Date;
                consulta = consulta.Where(r => r.FechaRegistro >= inicio);
            }

            if (hasta.HasValue)
            {
                DateTime fin = hasta.Value.Date.AddDays(1);
                consulta = consulta.Where(r => r.FechaRegistro < fin);
            }

            // POR ESTADO
            var estados = await consulta
                .GroupBy(r => r.IdEstado)
                .Select(g => new { IdEstado = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            int total = estados.Sum(e => e.Cantidad);

            var porEstado = Estados
                .Select(e => CrearItem(
                    e.Nombre,
                    estados.Where(x => x.IdEstado == e.Id)
                           .Sum(x => x.Cantidad),
                    total))
                .ToList();

            // POR TIPO
            var tipos = await (
                from r in consulta
                join t in _db.TiposReclamo.AsNoTracking()
                    on r.IdTipoReclamo equals t.IdTipoReclamo
                group r by t.NombreTipo into g
                orderby g.Count() descending
                select new { Nombre = g.Key, Cantidad = g.Count() }
            ).ToListAsync();

            var porTipo = tipos
                .Select(t => CrearItem(t.Nombre, t.Cantidad, total))
                .ToList();

            // CARGA DE OPERARIOS (SITUACION ACTUAL, SIN FILTRO DE FECHAS)
            var operarios = await (
                from o in _db.Operarios.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking()
                    on o.IdUsuario equals u.IdUsuario
                join t in _db.TiposReclamo.AsNoTracking()
                    on o.IdTipoReclamo equals t.IdTipoReclamo
                orderby t.NombreTipo, u.NombresApellidos
                select new
                {
                    o.IdUsuario,
                    Nombre = u.NombresApellidos,
                    Tipo = t.NombreTipo
                }
            ).ToListAsync();

            var asignados = await _db.Reclamos
                .AsNoTracking()
                .Where(r => r.IdOperario != null)
                .GroupBy(r => new { r.IdOperario, r.IdEstado })
                .Select(g => new
                {
                    g.Key.IdOperario,
                    g.Key.IdEstado,
                    Cantidad = g.Count()
                })
                .ToListAsync();

            var cargaOperarios = operarios.Select(o =>
            {
                int Contar(int estado) => asignados
                    .Where(a => a.IdOperario == o.IdUsuario
                                && a.IdEstado == estado)
                    .Sum(a => a.Cantidad);

                int enProceso = Contar(EnProceso);

                return new ReporteOperarioVM
                {
                    Nombre = o.Nombre,
                    Tipo = o.Tipo,
                    EnProceso = enProceso,
                    Resueltos = Contar(Resuelto),
                    Cerrados = Contar(Cerrado),
                    Disponible = enProceso == 0
                };
            }).ToList();

            // TIEMPO PROMEDIO DE RESOLUCION (SEGUN EL HISTORIAL)
            var resoluciones = await (
                from h in _db.HistorialReclamos.AsNoTracking()
                join r in consulta on h.IdReclamo equals r.IdReclamo
                where h.IdEstadoNuevo == Resuelto
                      && h.FechaCambio != null
                      && r.FechaRegistro != null
                select new
                {
                    Registro = r.FechaRegistro.Value,
                    Resuelto = h.FechaCambio.Value
                }
            ).ToListAsync();

            string tiempoPromedio = "Sin datos";

            if (resoluciones.Count > 0)
            {
                double horas = resoluciones.Average(x =>
                    Math.Max(0, (x.Resuelto - x.Registro).TotalHours));

                tiempoPromedio = FormatearDuracion(horas);
            }

            var vm = new ReporteVM
            {
                Desde = desde,
                Hasta = hasta,
                Total = total,
                PorEstado = porEstado,
                PorTipo = porTipo,
                Operarios = cargaOperarios,
                TiempoPromedioResolucion = tiempoPromedio,
                ReclamosConResolucion = resoluciones.Count
            };

            return View(vm);
        }

        private static ReporteItemVM CrearItem(
            string nombre, int cantidad, int total) => new()
            {
                Nombre = nombre,
                Cantidad = cantidad,
                Porcentaje = total == 0
                ? 0
                : (int)Math.Round(cantidad * 100.0 / total)
            };

        private static string FormatearDuracion(double horas)
        {
            if (horas < 1)
                return "Menos de 1 hora";

            if (horas < 48)
                return $"{horas:0.#} horas";

            return $"{horas / 24:0.#} días";
        }
    }
}
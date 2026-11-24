using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PyFinal_1.Data;
using PyFinal_1.ViewModels.Reclamos;

namespace PyFinal_1.ViewComponents;

public class EvidenciasViewComponent : ViewComponent
{
    private readonly ReclamosDbContext _db;

    public EvidenciasViewComponent(ReclamosDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(string codigo)
    {
        var evidencias = await (
            from e in _db.Evidencias.AsNoTracking()
            join r in _db.Reclamos.AsNoTracking()
                on e.IdReclamo equals r.IdReclamo
            where r.CodigoReclamo == codigo
            orderby e.IdEvidencia
            select new EvidenciaItemVM
            {
                Id = e.IdEvidencia,
                Nombre = e.NombreOriginal,
                Tamano = e.TamanoBytes
            }
        ).ToListAsync();

        return View(evidencias);
    }
}
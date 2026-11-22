namespace PyFinal_1.ViewModels.Reportes;

public class ReporteVM
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public int Total { get; set; }

    public List<ReporteItemVM> PorEstado { get; set; } = new();
    public List<ReporteItemVM> PorTipo { get; set; } = new();
    public List<ReporteOperarioVM> Operarios { get; set; } = new();

    public string TiempoPromedioResolucion { get; set; } = "Sin datos";
    public int ReclamosConResolucion { get; set; }

    public int Finalizados => PorEstado
        .Where(e => e.Nombre == "Resuelto" || e.Nombre == "Cerrado")
        .Sum(e => e.Cantidad);
}

public class ReporteItemVM
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int Porcentaje { get; set; }
}

public class ReporteOperarioVM
{
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int EnProceso { get; set; }
    public int Resueltos { get; set; }
    public int Cerrados { get; set; }
    public bool Disponible { get; set; }
}
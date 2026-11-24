namespace PyFinal_1.ViewModels.Reclamos;

public class EvidenciaItemVM
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public long Tamano { get; set; }

    public string TamanoLegible => Tamano switch
    {
        < 1024 => $"{Tamano} B",
        < 1024 * 1024 => $"{Tamano / 1024.0:0.#} KB",
        _ => $"{Tamano / 1024.0 / 1024.0:0.#} MB"
    };
}
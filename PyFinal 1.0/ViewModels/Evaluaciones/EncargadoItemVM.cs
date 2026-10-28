namespace PyFinal_1.ViewModels.Evaluaciones;

public class EncargadoItemVM
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoEncargado { get; set; } = string.Empty;
    public bool Disponible { get; set; }
}
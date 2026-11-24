using PyFinal_1.Models;

namespace PyFinal_1.Services;

public class EvidenciaService
{
    private readonly string _carpeta;

    public EvidenciaService(IWebHostEnvironment entorno)
    {
        // Fuera de wwwroot: los archivos no se pueden abrir por dirección directa
        _carpeta = Path.Combine(
            entorno.ContentRootPath, "App_Data", "evidencias");
    }

    // Comprueba que el contenido real coincida con la extensión
    public async Task<bool> ContenidoValidoAsync(
        IFormFile archivo, string extension)
    {
        byte[] cabecera = new byte[8];

        await using var lectura = archivo.OpenReadStream();
        int leidos = await lectura.ReadAsync(cabecera.AsMemory(0, cabecera.Length));

        if (leidos < 4)
            return false;

        return extension switch
        {
            ".jpg" or ".jpeg" =>
                cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF,

            ".png" =>
                cabecera[0] == 0x89 && cabecera[1] == 0x50 &&
                cabecera[2] == 0x4E && cabecera[3] == 0x47,

            ".pdf" =>
                cabecera[0] == 0x25 && cabecera[1] == 0x50 &&
                cabecera[2] == 0x44 && cabecera[3] == 0x46,

            _ => false
        };
    }

    // Guarda el archivo en disco con un nombre generado por el sistema
    public async Task<EvidenciaBD> GuardarAsync(
        IFormFile archivo, int idReclamo)
    {
        string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        string nombreGuardado = Guid.NewGuid().ToString("N") + extension;

        Directory.CreateDirectory(_carpeta);

        string ruta = Path.Combine(_carpeta, nombreGuardado);

        try
        {
            await using var destino = new FileStream(ruta, FileMode.CreateNew);
            await archivo.CopyToAsync(destino);
        }
        catch
        {
            Eliminar(nombreGuardado);
            throw;
        }

        string original = Path.GetFileName(archivo.FileName);

        if (original.Length > 200)
            original = original[^200..];

        return new EvidenciaBD
        {
            IdReclamo = idReclamo,
            NombreOriginal = original,
            NombreArchivo = nombreGuardado,
            TipoContenido = TipoContenido(extension),
            TamanoBytes = archivo.Length,
            FechaAdjunto = DateTime.Now
        };
    }

    public string RutaFisica(string nombreArchivo)
        => Path.Combine(_carpeta, Path.GetFileName(nombreArchivo));

    public void Eliminar(string nombreArchivo)
    {
        try
        {
            string ruta = RutaFisica(nombreArchivo);

            if (File.Exists(ruta))
                File.Delete(ruta);
        }
        catch
        {
            // Un archivo que no se puede borrar no debe romper el registro
        }
    }

    private static string TipoContenido(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
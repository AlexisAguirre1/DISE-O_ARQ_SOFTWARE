
namespace PyFinal_1._0.Domain
{
    // Patrón de diseño Singleton
    public sealed class ConfiguracionSistema
    {
        // Única instancia
        private static readonly ConfiguracionSistema instancia =
            new ConfiguracionSistema();

        // Constructor privado
        private ConfiguracionSistema()
        {
        }

        // Acceso global a la instancia
        public static ConfiguracionSistema Instancia
        {
            get { return instancia; }
        }

        // Configuración general
        public string NombreSistema { get; } =
            "Sistema de Quejas y Reclamos";

        public string EstadoInicialReclamo { get; } =
            "Pendiente";

        // Tipos de reclamos permitidos
        private readonly string[] tiposPermitidos =
        {
            "Facturacion",
            "Cobertura",
            "Atencion",
            "Equipos"
        };

        // Extensiones permitidas
        private readonly string[] extensionesPermitidas =
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };

        // Tamaño máximo de evidencia: 5 MB
        public long TamanoMaximoBytes { get; } =
            5 * 1024 * 1024;

        // Validar tipo de reclamo
        public bool EsTipoPermitido(string? tipo)
        {
            return tiposPermitidos.Contains(tipo);
        }

        // Validar extensión
        public bool EsExtensionPermitida(string? extension)
        {
            return extensionesPermitidas.Contains(extension);
        }
    }
}       
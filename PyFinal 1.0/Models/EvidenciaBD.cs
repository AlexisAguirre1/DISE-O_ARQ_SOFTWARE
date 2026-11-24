using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PyFinal_1.Models
{
    [Table("Evidencias")]
    public class EvidenciaBD
    {
        [Key]
        public int IdEvidencia { get; set; }

        public int IdReclamo { get; set; }

        public string NombreOriginal { get; set; } = string.Empty;

        public string NombreArchivo { get; set; } = string.Empty;

        public string TipoContenido { get; set; } = string.Empty;

        public long TamanoBytes { get; set; }

        public DateTime FechaAdjunto { get; set; }
    }
}
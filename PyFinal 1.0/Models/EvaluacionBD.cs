using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PyFinal_1.Models
{
    [Table("Evaluaciones")]
    public class EvaluacionBD
    {
        [Key]
        public int IdEvaluacion { get; set; }

        public int IdReclamo { get; set; }

        public int IdSupervisor { get; set; }

        public string Resultado { get; set; } = string.Empty;

        public string? Respuesta { get; set; }

        public DateTime FechaEvaluacion { get; set; }
    }
}
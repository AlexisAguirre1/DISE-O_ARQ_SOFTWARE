using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PyFinal_1.Models
{
    [Table("Impugnaciones")]
    public class ImpugnacionBD
    {
        [Key]
        public int IdImpugnacion { get; set; }

        public int IdReclamo { get; set; }

        public int IdSolicitante { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public DateTime FechaImpugnacion { get; set; }

        public string? Resultado { get; set; }

        public string? RespuestaSupervisor { get; set; }

        public int? IdSupervisor { get; set; }

        public DateTime? FechaResolucion { get; set; }
    }
}
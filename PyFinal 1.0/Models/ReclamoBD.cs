namespace PyFinal_1.Models
{
    public class ReclamoBD
    {
        public int IdReclamo { get; set; }

        public string CodigoReclamo { get; set; } = string.Empty;

        public int IdUsuario { get; set; }

        public int IdTipoReclamo { get; set; }

        public int IdEstado { get; set; }

        public int? IdOperario { get; set; }

        public string Detalle { get; set; } = string.Empty;

        public DateTime? FechaRegistro { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }
}
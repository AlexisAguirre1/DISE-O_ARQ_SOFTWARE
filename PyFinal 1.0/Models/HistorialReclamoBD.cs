namespace PyFinal_1.Models
{
    public class HistorialReclamoBD
    {
        public int IdHistorial { get; set; }

        public int IdReclamo { get; set; }

        public int IdEstadoAnterior { get; set; }

        public int IdEstadoNuevo { get; set; }

        public int IdUsuarioModificacion { get; set; }

        public string? Comentario { get; set; }

        public DateTime? FechaCambio { get; set; }
    }
}
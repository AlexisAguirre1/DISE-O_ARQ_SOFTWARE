using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PyFinal_1.ViewModels.Reclamos
{
    public class CrearReclamoVM
    {
        // DATOS DE LA CUENTA (solo lectura): los carga el controlador, no el formulario
        public string NombreCompleto { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string Dni { get; set; } = string.Empty;

        public string DniEnmascarado =>
            Dni.Length == 8 ? "••••" + Dni[^4..] : "No registrado";

        // TIPO DE RECLAMO
        [Required(ErrorMessage = "Seleccione el tipo de reclamo.")]
        [Display(Name = "Tipo de reclamo")]
        public string TipoReclamo { get; set; } = string.Empty;

        // DESCRIPCIÓN DEL RECLAMO
        [Required(ErrorMessage = "Describa el reclamo.")]
        [StringLength(500, MinimumLength = 20,
            ErrorMessage = "La descripción debe tener entre 20 y 500 caracteres.")]
        [Display(Name = "Detalle del reclamo")]
        public string Descripcion { get; set; } = string.Empty;

        // ARCHIVO DE EVIDENCIA
        [Display(Name = "Evidencia (opcional)")]
        public IFormFile? Evidencia { get; set; }
    }
}
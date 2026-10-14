using System.ComponentModel.DataAnnotations;

namespace PyFinal_1.ViewModels.Reclamos
{
    public class CrearReclamoVM
    {
        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos.")]
        [Display(Name = "DNI del solicitante")]
        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Nombres y apellidos")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required, EmailAddress(ErrorMessage = "Correo no válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione el tipo de reclamo.")]
        [Display(Name = "Tipo de reclamo")]
        public string TipoReclamo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Describa el reclamo.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "La descripción debe tener entre 20 y 1000 caracteres.")]
        [Display(Name = "Detalle del reclamo")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Evidencia (opcional)")]
        public IFormFile? Evidencia { get; set; }
    }
}

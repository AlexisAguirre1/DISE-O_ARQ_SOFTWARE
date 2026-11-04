
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PyFinal_1.ViewModels.Reclamos
{
    public class CrearReclamoVM
    {
        // DNI DEL SOLICITANTE
        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [RegularExpression(@"^[0-9]{8}$",
            ErrorMessage = "El DNI debe tener 8 dígitos numéricos.")]
        [Display(Name = "DNI del solicitante")]
        public string Dni { get; set; } = string.Empty;

        // NOMBRES Y APELLIDOS
        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100, MinimumLength = 3,
            ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        [Display(Name = "Nombres y apellidos")]
        public string NombreCompleto { get; set; } = string.Empty;

        // CORREO ELECTRÓNICO
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [StringLength(100,
            ErrorMessage = "El correo no puede superar 100 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

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

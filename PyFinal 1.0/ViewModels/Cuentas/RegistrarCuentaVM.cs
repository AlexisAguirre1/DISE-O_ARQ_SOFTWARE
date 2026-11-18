using System.ComponentModel.DataAnnotations;
namespace PyFinal_1.ViewModels.Cuentas;

public class RegistrarCuentaVM
{
    [Required(ErrorMessage = "Ingresa tus nombres y apellidos.")]
    [StringLength(100, MinimumLength = 3,
        ErrorMessage = "Debe tener entre 3 y 100 caracteres.")]
    public string NombresApellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(100, ErrorMessage = "El correo no puede superar 100 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu DNI.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos.")]
    public string DNI { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contraseña.")]
    [StringLength(64, MinimumLength = 8,
        ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
        ErrorMessage = "Debe incluir letras y números.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}
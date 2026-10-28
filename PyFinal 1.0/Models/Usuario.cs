namespace PyFinal_1.Models
{
    public class UsuarioBD
    {
        public int IdUsuario { get; set; }

        public string NombresApellidos { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public int IdRol { get; set; }
    }
}
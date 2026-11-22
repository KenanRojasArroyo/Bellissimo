namespace Bellissimo.Entidades;

// Personas que usan la página (dueño y encargado).
public class Usuario
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;

    // Dueno o Encargado
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

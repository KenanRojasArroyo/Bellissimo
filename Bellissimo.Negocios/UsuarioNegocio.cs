using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

// Dueño y encargado. Se usa para elegir quién hace una corrección o genera un pago.
public class UsuarioNegocio
{
    private readonly UsuarioDatos _datos = new();

    public List<Usuario> ListarActivos()
        => _datos.ListarActivos();
}

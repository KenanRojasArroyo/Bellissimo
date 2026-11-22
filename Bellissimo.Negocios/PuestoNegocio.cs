using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

public class PuestoNegocio
{
    private readonly PuestoDatos _datos = new();

    public List<Puesto> Listar()
        => _datos.Listar();
}

using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

// Catálogo de puestos: alta, consulta y edición. No se borran (RN15):
// la base no da permiso de DELETE sobre puesto.
public class PuestoNegocio
{
    private readonly PuestoDatos _datos = new();

    public List<Puesto> Listar()
        => _datos.Listar();

    public Puesto? BuscarPorId(int idPuesto)
    {
        if (idPuesto <= 0)
            return null;

        return _datos.BuscarPorId(idPuesto);
    }

    public int Agregar(Puesto puesto)
    {
        Validar(puesto, 0);
        return ErroresBaseDatos.Ejecutar(() => _datos.Agregar(puesto));
    }

    public bool Modificar(Puesto puesto)
    {
        if (puesto.IdPuesto <= 0)
            throw new ArgumentException("Puesto no válido.");

        Validar(puesto, puesto.IdPuesto);
        return ErroresBaseDatos.Ejecutar(() => _datos.Modificar(puesto));
    }

    private void Validar(Puesto puesto, int idPuestoExcluir)
    {
        puesto.Nombre = puesto.Nombre?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(puesto.Nombre))
            throw new ArgumentException("El nombre del puesto es obligatorio.");

        if (puesto.Nombre.Length > 50)
            throw new ArgumentException(
                "El nombre del puesto no puede exceder los 50 caracteres.");

        if (_datos.ExisteNombre(puesto.Nombre, idPuestoExcluir))
            throw new ArgumentException("Ya existe un puesto con ese nombre.");
    }
}

using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

public class PersonalNegocio
{
    // DECIMAL(10,2) de empleado.sueldo_base_semanal
    private const decimal SueldoMaximo = 99_999_999.99m;

    private readonly PersonalDatos _datos = new();
    private readonly PuestoDatos _puestoDatos = new();

    public List<Personal> Listar()
        => _datos.Listar();

    public List<Personal> Buscar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return _datos.Listar();

        return _datos.Buscar(texto.Trim());
    }

    public Personal? BuscarPorId(int idPersonal)
    {
        if (idPersonal <= 0)
            return null;

        return _datos.BuscarPorId(idPersonal);
    }

    public int Agregar(Personal personal)
    {
        Normalizar(personal);
        Validar(personal);
        return ErroresBaseDatos.Ejecutar(() => _datos.Agregar(personal));
    }

    public bool Modificar(Personal personal)
    {
        if (personal.IdPersonal <= 0)
            throw new ArgumentException("Empleado no válido.");

        Normalizar(personal);
        Validar(personal);
        return ErroresBaseDatos.Ejecutar(() => _datos.Modificar(personal));
    }

    public bool Eliminar(int idPersonal)
    {
        if (idPersonal <= 0)
            throw new ArgumentException(
                "Empleado no válido.");

        Personal? personal =
            _datos.BuscarPorId(idPersonal);

        if (personal is null)
            return false;

        if (_datos.TieneHistorial(idPersonal))
        {
            throw new ReglaNegocioException(
                "No se puede eliminar al empleado porque tiene " +
                "checadas, ausencias o pagos registrados. " +
                "Para darlo de baja, desactívelo desde Editar.");
        }

        return ErroresBaseDatos.Ejecutar(() => _datos.Eliminar(idPersonal));
    }

    // Asigna o cambia la clave personal del checador. Sólo se guarda el hash.
    public bool AsignarClave(int idPersonal, string clave)
    {
        if (idPersonal <= 0)
            throw new ArgumentException("Empleado no válido.");

        clave = clave?.Trim() ?? "";

        if (clave.Length < 4 || clave.Length > 8 || !clave.All(char.IsAsciiDigit))
            throw new ArgumentException("La clave debe tener de 4 a 8 dígitos.");

        if (_datos.BuscarPorId(idPersonal) is null)
            return false;

        return _datos.ActualizarClaveHash(idPersonal, ClaveHasher.Generar(clave));
    }

    private static void Normalizar(Personal personal)
    {
        personal.Nombre = personal.Nombre?.Trim() ?? "";
        personal.ApellidoPaterno = personal.ApellidoPaterno?.Trim() ?? "";
        personal.Telefono = personal.Telefono?.Trim() ?? "";

        personal.ApellidoMaterno =
            string.IsNullOrWhiteSpace(personal.ApellidoMaterno)
                ? null
                : personal.ApellidoMaterno.Trim();
    }

    private void Validar(Personal personal)
    {
        if (string.IsNullOrWhiteSpace(personal.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (personal.Nombre.Length > 60)
            throw new ArgumentException(
                "El nombre no puede exceder los 60 caracteres.");

        if (string.IsNullOrWhiteSpace(personal.ApellidoPaterno))
            throw new ArgumentException(
                "El apellido paterno es obligatorio.");

        if (personal.ApellidoPaterno.Length > 60)
            throw new ArgumentException(
                "El apellido paterno no puede exceder los 60 caracteres.");

        if (personal.ApellidoMaterno is { Length: > 60 })
            throw new ArgumentException(
                "El apellido materno no puede exceder los 60 caracteres.");

        if (string.IsNullOrWhiteSpace(personal.Telefono))
            throw new ArgumentException("El teléfono es obligatorio.");

        if (personal.Telefono.Length > 20)
            throw new ArgumentException(
                "El teléfono no puede exceder los 20 caracteres.");

        if (personal.SueldoBase <= 0)
            throw new ArgumentException(
                "El sueldo base semanal debe ser mayor a 0.");

        if (personal.SueldoBase > SueldoMaximo)
            throw new ArgumentException(
                "El sueldo base semanal excede el máximo permitido.");

        if (personal.IdPuesto <= 0 || !_puestoDatos.Existe(personal.IdPuesto))
            throw new ArgumentException("Seleccione un puesto válido.");
    }
}

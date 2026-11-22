using Bellissimo.Datos;

namespace Bellissimo.Negocios;

// Convierte los errores conocidos de MySQL (Sql/CONTRATO_BD.md §5) en
// ReglaNegocioException con un mensaje que el usuario puede entender.
internal static class ErroresBaseDatos
{
    public static ReglaNegocioException Traducir(BaseDatosException ex)
    {
        string mensaje = ex.Numero switch
        {
            // SIGNAL de un procedimiento o trigger: el texto ya es para el usuario.
            BaseDatosException.ReglaNegocio => ex.Message,
            BaseDatosException.Duplicado =>
                "Ya existe un registro con ese dato.",
            BaseDatosException.TieneRelacionados =>
                "No se puede borrar: tiene registros relacionados. Desactívelo en lugar de borrarlo.",
            BaseDatosException.ReferenciaInexistente =>
                "El empleado, puesto, semana o usuario indicado no existe.",
            BaseDatosException.RestriccionCheck =>
                "Los datos no cumplen las reglas de la base de datos (revise montos, fechas y el orden de las horas).",
            _ => "No fue posible completar la operación."
        };

        return new ReglaNegocioException(mensaje, ex);
    }

    public static T Ejecutar<T>(Func<T> accion)
    {
        try
        {
            return accion();
        }
        catch (BaseDatosException ex)
        {
            throw Traducir(ex);
        }
    }

    public static void Ejecutar(Action accion)
    {
        try
        {
            accion();
        }
        catch (BaseDatosException ex)
        {
            throw Traducir(ex);
        }
    }
}

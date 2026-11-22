using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

// La entidad Personal se guarda en la tabla empleado (ver Sql/CONTRATO_BD.md):
// IdPersonal = id_empleado, SueldoBase = sueldo_base_semanal,
// FechaRegistro = fecha_ingreso, NombrePuesto = puesto.nombre.
public class PersonalDatos
{
    private const string SelectBase = """
        SELECT e.id_empleado, e.nombre, e.apellido_paterno,
               e.apellido_materno, e.telefono, e.id_puesto,
               p.nombre AS nombre_puesto, e.sueldo_base_semanal,
               e.activo, e.fecha_ingreso
        FROM empleado e
        INNER JOIN puesto p
            ON p.id_puesto = e.id_puesto
        """;

    public List<Personal> Listar()
    {
        List<Personal> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            ORDER BY e.apellido_paterno, e.nombre;
            """;

        using MySqlCommand cmd = new(sql, cn);
        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(Mapear(dr));
        }

        return lista;
    }

    public List<Personal> Buscar(string texto)
    {
        List<Personal> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            WHERE e.nombre LIKE @texto
               OR e.apellido_paterno LIKE @texto
               OR e.apellido_materno LIKE @texto
               OR CONCAT_WS(' ', e.nombre, e.apellido_paterno,
                            e.apellido_materno) LIKE @texto
               OR e.telefono LIKE @texto
               OR p.nombre LIKE @texto
            ORDER BY e.apellido_paterno, e.nombre;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@texto", $"%{texto}%");

        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(Mapear(dr));
        }

        return lista;
    }

    public Personal? BuscarPorId(int idPersonal)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            WHERE e.id_empleado = @idPersonal;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idPersonal", idPersonal);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? Mapear(dr) : null;
    }

    public int Agregar(Personal personal)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        // fecha_ingreso es obligatoria en la tabla y la interfaz no la captura:
        // se registra con la fecha del servidor.
        const string sql = """
            INSERT INTO empleado
            (id_puesto, nombre, apellido_paterno, apellido_materno,
             telefono, fecha_ingreso, sueldo_base_semanal, activo)
            VALUES
            (@idPuesto, @nombre, @apellidoPaterno, @apellidoMaterno,
             @telefono, CURDATE(), @sueldoBase, @activo);
            """;

        using MySqlCommand cmd = new(sql, cn);

        AgregarParametros(cmd, personal);
        cmd.ExecuteNonQuery();

        return Convert.ToInt32(cmd.LastInsertedId);
    }

    public bool Modificar(Personal personal)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        // No se modifican fecha_ingreso, clave_hash ni id_huella.
        const string sql = """
            UPDATE empleado
            SET id_puesto = @idPuesto,
                nombre = @nombre,
                apellido_paterno = @apellidoPaterno,
                apellido_materno = @apellidoMaterno,
                telefono = @telefono,
                sueldo_base_semanal = @sueldoBase,
                activo = @activo
            WHERE id_empleado = @idPersonal;
            """;

        using MySqlCommand cmd = new(sql, cn);

        AgregarParametros(cmd, personal);
        cmd.Parameters.AddWithValue("@idPersonal", personal.IdPersonal);

        return cmd.ExecuteNonQuery() > 0;
    }

    // Historial que impide el borrado físico (FK con ON DELETE RESTRICT):
    // checadas, ausencias y pagos semanales.
    public bool TieneHistorial(int idPersonal)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM registro_asistencia
                 WHERE id_empleado = @idPersonal)
              + (SELECT COUNT(*) FROM ausencia
                 WHERE id_empleado = @idPersonal)
              + (SELECT COUNT(*) FROM pago_semanal
                 WHERE id_empleado = @idPersonal);
            """;

        using MySqlCommand cmd = new(sql, cn);

        cmd.Parameters.AddWithValue(
            "@idPersonal",
            idPersonal);

        int total =
            Convert.ToInt32(
                cmd.ExecuteScalar());

        return total > 0;
    }

    public bool Eliminar(int idPersonal)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            DELETE FROM empleado
            WHERE id_empleado = @idPersonal;
            """;

        using MySqlCommand cmd = new(sql, cn);

        cmd.Parameters.AddWithValue(
            "@idPersonal",
            idPersonal);

        return cmd.ExecuteNonQuery() > 0;
    }

    private static void AgregarParametros(
        MySqlCommand cmd,
        Personal personal)
    {
        cmd.Parameters.AddWithValue("@idPuesto", personal.IdPuesto);
        cmd.Parameters.AddWithValue("@nombre", personal.Nombre);

        cmd.Parameters.AddWithValue(
            "@apellidoPaterno", personal.ApellidoPaterno);

        cmd.Parameters.AddWithValue(
            "@apellidoMaterno",
            (object?)personal.ApellidoMaterno ?? DBNull.Value);

        cmd.Parameters.AddWithValue("@telefono", personal.Telefono);
        cmd.Parameters.AddWithValue("@sueldoBase", personal.SueldoBase);
        cmd.Parameters.AddWithValue("@activo", personal.Activo);
    }

    private static Personal Mapear(MySqlDataReader dr)
    {
        return new Personal
        {
            IdPersonal = Convert.ToInt32(dr["id_empleado"]),
            Nombre = Convert.ToString(dr["nombre"]) ?? "",
            ApellidoPaterno =
                Convert.ToString(dr["apellido_paterno"]) ?? "",
            ApellidoMaterno =
                dr["apellido_materno"] == DBNull.Value
                    ? null
                    : Convert.ToString(dr["apellido_materno"]),
            Telefono =
                dr["telefono"] == DBNull.Value
                    ? ""
                    : Convert.ToString(dr["telefono"]) ?? "",
            IdPuesto = Convert.ToInt32(dr["id_puesto"]),
            NombrePuesto = Convert.ToString(dr["nombre_puesto"]),
            SueldoBase = Convert.ToDecimal(dr["sueldo_base_semanal"]),
            Activo = Convert.ToBoolean(dr["activo"]),
            FechaRegistro = Convert.ToDateTime(dr["fecha_ingreso"])
        };
    }
}

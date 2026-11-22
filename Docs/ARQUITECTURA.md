# Arquitectura

El sistema usa una arquitectura por capas. Cada capa sólo conoce a la capa inmediata
inferior y a `Bellissimo.Entidades`. Diagrama: [`Diagramas/arquitectura.png`](../Diagramas/arquitectura.png).

## Bellissimo.Web
Interfaz y Razor Pages.

- Páginas de `Personal` (Index, Nuevo, Editar, Detalle, Eliminar) con Bootstrap.
- Validaciones de formulario con Data Annotations y `ModelState`.
- Mensajes de éxito con `TempData`; errores visibles con la propiedad `Error`.
- `Program.cs` lee `ConnectionStrings:Bellissimo` (User Secrets) e inicializa Negocios con
  `ConfiguracionNegocio.Inicializar`.
- **No** contiene SQL ni usa `MySqlConnection`.
- Referencia a: `Bellissimo.Negocios`, `Bellissimo.Entidades`.

## Bellissimo.Negocios
Reglas y validaciones.

- `PersonalNegocio`: normaliza espacios; valida nombre, apellidos, teléfono, sueldo base
  semanal mayor a 0 y que el puesto exista; impide eliminar a un empleado con historial
  (checadas, ausencias o pagos).
- `PuestoNegocio`: lista los puestos de la tabla `puesto` para el `<select>`.
- `ReglaNegocioException`: distingue una regla funcional (mensaje para el usuario) de un
  error técnico.
- `ConfiguracionNegocio`: recibe la cadena de conexión desde Web.
- **No** contiene HTML ni SQL.
- Referencia a: `Bellissimo.Datos`, `Bellissimo.Entidades`.

## Bellissimo.Datos
ADO.NET y MySQL (`MySql.Data 26.10.0`).

- `Conexion`: crea `MySqlConnection` con la cadena configurada.
- `PersonalDatos`: `SELECT`, `INSERT`, `UPDATE` y `DELETE` sobre la tabla `empleado`
  (con `INNER JOIN puesto`) usando parámetros (`@idPersonal`, `@texto`, `@idPuesto`, …).
  Nunca se concatena un dato del usuario dentro del SQL.
- `PuestoDatos`: lista puestos y comprueba que un `@idPuesto` exista.
- `TieneHistorial()` apoya la regla de eliminación.
- Referencia a: `Bellissimo.Entidades` y al paquete `MySql.Data`.

## Bellissimo.Entidades
Modelos compartidos.

- `Personal` y `Puesto`, con Data Annotations (`Required`, `StringLength`, `Range`,
  `Display`).
- **No** depende de ninguna otra capa.

## Flujo

Web
↓
Negocios
↓
Datos
↓
MySQL (`belissimo`, usuario `app_floreria`)

## Correspondencia entidad ↔ tabla

La entidad se llama `Personal` y la tabla `empleado`. La traducción vive sólo en
`PersonalDatos`, así Web y Negocios no conocen nombres de columnas.

| `Personal` | `empleado` |
| --- | --- |
| `IdPersonal` | `id_empleado` |
| `IdPuesto` / `NombrePuesto` | `id_puesto` / `puesto.nombre` |
| `SueldoBase` | `sueldo_base_semanal` |
| `Activo` | `activo` |
| `FechaRegistro` | `fecha_ingreso` (`CURDATE()` al registrar) |

`clave_hash` e `id_huella` pertenecen al checador y no se leen ni se modifican desde
Personal.

## Ejemplo de flujo: registrar un empleado

1. **Web** — `Personal/Nuevo` recibe el formulario. Si `ModelState` no es válido (por
   ejemplo, sueldo 0 o sin puesto), regresa la página con los mensajes y no continúa.
2. **Negocios** — `PersonalNegocio.Agregar(personal)` quita espacios y vuelve a validar los
   datos, incluido que el puesto exista en la tabla.
3. **Datos** — `PersonalDatos.Agregar(personal)` ejecuta
   `INSERT INTO empleado … VALUES (@idPuesto, @nombre, …, CURDATE(), @sueldoBase, @activo)`.
4. **MySQL** — guarda el registro; `ck_empleado_sueldo` y `fk_empleado_puesto` respaldan
   las mismas reglas.
5. **Web** — redirige al listado con un mensaje de éxito en `TempData`.

## Ejemplo de flujo: eliminar un empleado

1. **Web** — `Personal/Eliminar` (`OnPost`) carga el empleado dentro de un `try` y llama a
   `PersonalNegocio.Eliminar(id)`.
2. **Negocios** — si `PersonalDatos.TieneHistorial(id)` es verdadero, lanza
   `ReglaNegocioException("No se puede eliminar al empleado porque tiene checadas,
   ausencias o pagos registrados. Para darlo de baja, desactívelo desde Editar.")`.
3. **Datos** — si no tiene historial, ejecuta
   `DELETE FROM empleado WHERE id_empleado = @idPersonal`.
4. **Web** — la regla se muestra con `Model.Error` y se oculta el botón Eliminar. Un error
   técnico se registra con `Debug.WriteLine(ex)` y el usuario sólo ve un mensaje amigable.
5. **MySQL** — las llaves foráneas `fk_registro_empleado`, `fk_ausencia_empleado` y
   `fk_pago_empleado` (`ON DELETE RESTRICT`) protegen la integridad aunque alguien intente
   borrar desde fuera del sistema.

La baja normal es **lógica**: en `Editar` se apaga el interruptor *Activo* (`activo = 0`).

## Manejo de errores

| Situación | Tratamiento |
| --- | --- |
| Formulario Nuevo / Editar inválido | Data Annotations + `ModelState`; `ArgumentException` de Negocios → `ModelState`; se conservan los datos capturados |
| Regla de negocio al eliminar | `ReglaNegocioException` → `Error` → `Model.Error` |
| Error técnico | `catch (Exception ex)` → `Debug.WriteLine(ex)` → mensaje amigable |

Nunca se muestra al usuario `ex.ToString()`, el stack trace, la cadena de conexión ni el
mensaje técnico de MySQL.

## Validaciones del DBA cubiertas (`Sql/validaciones_interfaz.md`)

| # | Dónde se cumple |
| --- | --- |
| V01 Sueldo semanal > 0 | `Personal.SueldoBase` `[Range]` + `PersonalNegocio.Validar` |
| V02 Nombre, apellido paterno (60) y teléfono (20) | Data Annotations + `PersonalNegocio.Validar` |
| V03 Puesto desde la tabla `puesto` | `PuestoNegocio.Listar` en Nuevo / Editar + `PuestoDatos.Existe` |
| V04 Desactivar en lugar de borrar | Regla de `PersonalNegocio.Eliminar` + interruptor *Activo* en Editar |
| V15 Siempre parámetros | Todo `Bellissimo.Datos` |

## Base de datos

Base `belissimo` (MySQL 8.0), 11 tablas. La interfaz Web de esta versión usa `empleado` y
`puesto`, y consulta `registro_asistencia`, `ausencia` y `pago_semanal` sólo para la regla
de eliminación. La aplicación se conecta con `app_floreria` (mínimo privilegio,
`Sql/05_usuario_app.sql`), que tiene los permisos necesarios para estas operaciones.

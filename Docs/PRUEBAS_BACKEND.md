# Pruebas Backend

Responsable: Troy Leonardo Moreno Calderón (Backend / Líder) — rama `backend`.

## Arquitectura

- Web sin SQL: Sí. Búsqueda en `Bellissimo.Web/**/*.cs` y `*.cshtml` de `INSERT INTO`,
  `DELETE FROM`, `UPDATE` y `SELECT` (coincidiendo mayúsculas): sin coincidencias.
- Web sin MySqlConnection: Sí. Sin coincidencias de `MySqlConnection`, `MySqlCommand`,
  `MySqlDataReader` ni `MySql` en `Bellissimo.Web`. `Bellissimo.Web.csproj` sólo
  referencia `Bellissimo.Entidades` y `Bellissimo.Negocios`.
- Negocios usa Datos: Sí. `PersonalNegocio` usa `PersonalDatos` y `PuestoDatos`;
  `PuestoNegocio` usa `PuestoDatos`. Negocios no contiene HTML ni SQL.
- Datos usa MySql.Data: Sí. `Bellissimo.Datos` referencia el paquete `MySql.Data 26.10.0`
  (la misma versión con la que el DBA probó el contrato) y es la única capa con
  `MySqlConnection`.
- Entidades no depende de otras capas: Sí.

## Reglas

- Empleado con historial: `PersonalNegocio.Eliminar` valida el Id, regresa `false` si el
  empleado no existe y lanza `ReglaNegocioException` si `PersonalDatos.TieneHistorial`
  encuentra checadas (`registro_asistencia`), ausencias (`ausencia`) o pagos
  (`pago_semanal`). Mensaje: *"No se puede eliminar al empleado porque tiene checadas,
  ausencias o pagos registrados. Para darlo de baja, desactívelo desde Editar."*
  Corresponde a RN15 (FK con `ON DELETE RESTRICT`, baja lógica con `activo = 0`).
- Sueldo base semanal mayor a 0: `PersonalNegocio.Validar` (RN02, `ck_empleado_sueldo`).
- Puesto válido: `PersonalNegocio.Validar` comprueba que `IdPuesto` exista en `puesto`
  (evita el error 1452 de la FK `fk_empleado_puesto`).
- Campos obligatorios y longitudes: nombre (60), apellido paterno (60), apellido materno
  (60, opcional), teléfono (20). Coinciden con las Data Annotations de `Personal` y con
  las columnas de `empleado`.
- `ReglaNegocioException` existe en `Bellissimo.Negocios` y la capa Web muestra su mensaje
  mediante la propiedad `Error`.

### Revisión de `PersonalNegocio`

| Método | Resultado |
| --- | --- |
| Listar | OK (incluye `NombrePuesto` con `INNER JOIN puesto`) |
| Buscar | OK (texto vacío → Listar; `Trim`; busca en nombre, apellidos, nombre completo, teléfono y puesto) |
| BuscarPorId | OK (Id ≤ 0 → `null`, la página responde `NotFound`) |
| Agregar | OK, normaliza espacios, valida y regresa el Id generado |
| Modificar | OK, valida Id y datos; no toca `fecha_ingreso`, `clave_hash` ni `id_huella` |
| Eliminar | OK, valida Id y bloquea empleados con historial |

### Revisión de `PuestoNegocio`

| Método | Resultado |
| --- | --- |
| Listar | OK, alimenta el `<select>` de Nuevo y Editar con la tabla `puesto` |

### Correspondencia entidad ↔ tabla (se traduce en `PersonalDatos`)

| `Personal` | `empleado` |
| --- | --- |
| `IdPersonal` | `id_empleado` |
| `Nombre`, `ApellidoPaterno`, `ApellidoMaterno`, `Telefono` | mismas columnas |
| `IdPuesto` / `NombrePuesto` | `id_puesto` / `puesto.nombre` |
| `SueldoBase` | `sueldo_base_semanal` |
| `Activo` | `activo` (`BOOLEAN`) |
| `FechaRegistro` | `fecha_ingreso` (al registrar se usa `CURDATE()`) |

## Consultas parametrizadas

- PersonalDatos: Sí. `Buscar`, `BuscarPorId`, `Agregar`, `Modificar`, `TieneHistorial` y
  `Eliminar` usan `@texto`, `@idPersonal`, `@idPuesto`, `@nombre`, `@apellidoPaterno`,
  `@apellidoMaterno`, `@telefono`, `@sueldoBase` y `@activo` con `AddWithValue`.
- PuestoDatos: Sí. `Existe` usa `@idPuesto`.
- La única concatenación es `SelectBase + """..."""`: dos constantes de compilación
  (`const string`), sin ningún dato del usuario.

## Manejo de errores

- Formularios Nuevo / Editar: `ArgumentException` → `ModelState`; error técnico →
  `Debug.WriteLine(ex)` + mensaje amigable en `ModelState`. Se conservan los datos
  capturados.
- Index / Detalle / carga inicial: `catch (Exception ex)` → `Debug.WriteLine(ex)` +
  propiedad `Error`.
- Eliminar: `ReglaNegocioException` → `Error`; error técnico → `Debug.WriteLine(ex)` +
  `Error` con mensaje amigable. La carga del empleado queda dentro del `try` del `OnPost`.
- Ninguna página muestra `ex.ToString()`, stack trace, cadena de conexión ni mensaje
  técnico de MySQL.

### Prueba sin base de datos (sin User Secrets) — 2026-11-22

Se ejecutó `Bellissimo.Web` en Development **sin** `ConnectionStrings:Bellissimo`:

| Página | HTTP | Mensaje visible |
| --- | :---: | --- |
| `/` | 200 | — |
| `/Personal` | 200 | No fue posible consultar el personal. Verifique la conexión con MySQL. |
| `/Personal/Nuevo` | 200 | No fue posible cargar los puestos. Verifique la conexión con MySQL. |
| `/Personal/Detalle/1` | 200 | No fue posible consultar el detalle del empleado. |
| `/Personal/Editar/1` | 200 | No fue posible consultar el empleado. Verifique la conexión con MySQL. |
| `/Personal/Eliminar/1` (GET) | 200 | No fue posible consultar el empleado. Verifique la conexión con MySQL. |
| `/Personal/Eliminar/1` (POST) | 200 | No fue posible eliminar el empleado. (botón Eliminar oculto) |

Antes de la corrección, el `POST` de Eliminar habría terminado en página 500.

### Prueba de integración Negocios → Datos → MySQL

Se ejecuta con la cadena de User Secrets de `Bellissimo.Web` y deja la base como estaba
(el empleado temporal de prueba se crea y se elimina).

| No. | Caso | Esperado | Resultado |
| ---: | --- | --- | :---: |
| 1 | `ProbarConexion` | Cuenta empleados | Pendiente |
| 2 | `PuestoNegocio.Listar` | Recepcionista, Repartidor, Florista | Pendiente |
| 3 | `Listar` | Empleados con `NombrePuesto` | Pendiente |
| 4 | `Buscar("Ana")`, `Buscar("Florista")`, `Buscar("Ana Torres")` | Filtra por nombre, puesto y nombre completo | Pendiente |
| 5 | `Buscar("' OR 1=1 -- ")` | 0 resultados (sin inyección) | Pendiente |
| 6 | `BuscarPorId` existente / `99999` / `0` | Empleado / `null` / `null` | Pendiente |
| 7 | `Agregar` con nombre vacío, sueldo 0, puesto 999, teléfono vacío | `ArgumentException` con el mensaje de la regla | Pendiente |
| 8 | `Agregar` válido | Id nuevo, `fecha_ingreso` = hoy, `activo = 1` | Pendiente |
| 9 | `Modificar` (baja lógica `Activo = false`, cambio de puesto) | `true` y cambios guardados | Pendiente |
| 10 | `Eliminar` a María (tiene checadas en `Sql/02_datos.sql`) | `ReglaNegocioException`; sigue existiendo | Pendiente |
| 11 | `Eliminar(0)` / `Eliminar(99999)` | `ArgumentException` / `false` | Pendiente |
| 12 | `Eliminar` del empleado temporal | `true`; ya no existe | Pendiente |

## `Program.cs`

- Lee `ConnectionStrings:Bellissimo`: Sí (nombre acordado en `Sql/conexion_plantilla.md`).
- Inicializa Negocios con `ConfiguracionNegocio.Inicializar`: Sí.
- Sin contraseña escrita: Sí. La cadena viene de User Secrets; `appsettings.json` y
  `appsettings.Development.json` no tienen cadena.
- Arranque no bloqueante si falta la cadena: Sí (sólo `Debug.WriteLine`; verificado arriba).
- Un solo `WebApplication.CreateBuilder(args)` y un solo `AddRazorPages()`: Sí.

## Compilación

- Antes de corregir (`main` en `2171fd2`): 5 errores `CS0234` (falta `Bellissimo.Negocios`).
- Después de corregir: 0 errores, 0 advertencias.

## Correcciones realizadas

- `Bellissimo.Datos`: se agregó `MySql.Data` y se crearon `Conexion`, `PersonalDatos` y
  `PuestoDatos`.
- `Bellissimo.Negocios`: se crearon `PersonalNegocio`, `PuestoNegocio`,
  `ReglaNegocioException` y `ConfiguracionNegocio`.
- `Bellissimo.Web/Program.cs`: lectura de `ConnectionStrings:Bellissimo` e inicialización
  de Negocios.
- `Personal/Eliminar.cshtml.cs` — `OnPost`: la carga del empleado (`BuscarPorId`) se movió
  dentro del `try`, para que una falla de MySQL muestre "No fue posible eliminar el
  empleado." en lugar de una página 500.
- `Personal/Nuevo.cshtml.cs` y `Personal/Editar.cshtml.cs` — `CargarPuestos`: la lista
  escrita a mano se reemplazó por `PuestoNegocio.Listar()` (la línea que Frontend dejó
  comentada). Corrige que el puesto 3 aparecía como "Encargado de Florista" cuando en la
  tabla se llama "Florista". El diseño de las vistas no se modificó.

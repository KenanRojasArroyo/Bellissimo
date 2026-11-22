# Pruebas Backend

Responsable: Troy Leonardo Moreno Calderón (Backend / Líder) — rama `backend`.

## Arquitectura

- Web sin SQL: Sí. Búsqueda en `Bellissimo.Web/**/*.cs` y `*.cshtml` de `INSERT INTO`,
  `DELETE FROM`, `UPDATE` y `SELECT` (coincidiendo mayúsculas): sin coincidencias.
- Web sin MySqlConnection: Sí. Sin coincidencias de `MySqlConnection`, `MySqlCommand`,
  `MySqlDataReader` ni `MySql` en `Bellissimo.Web`. `Bellissimo.Web.csproj` sólo
  referencia `Bellissimo.Entidades` y `Bellissimo.Negocios`.
- Negocios usa Datos: Sí. Cada clase de Negocios usa sólo clases de Datos
  (`PersonalNegocio` → `PersonalDatos`, `PuestoDatos`; `PagoNegocio` → `PagoDatos`,
  `SemanaDatos`, …). Negocios no contiene HTML, SQL ni referencias a `MySql`
  (búsqueda de `MySql`, `SELECT`, `INSERT INTO`, `DELETE FROM`: sin coincidencias).
- Los errores de MySQL se convierten en Datos a `BaseDatosException` (número + mensaje), y
  Negocios los traduce a `ReglaNegocioException`; así Negocios no depende de `MySql.Data`.
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

| AsignarClave | OK, clave de 4 a 8 dígitos; guarda sólo el hash PBKDF2-SHA256 (RN16) |

### Revisión de `PuestoNegocio`

| Método | Resultado |
| --- | --- |
| Listar | OK, alimenta el `<select>` de Nuevo y Editar con la tabla `puesto` |
| BuscarPorId | OK |
| Agregar / Modificar | OK, nombre obligatorio, máximo 50 y único |
| Eliminar | No existe a propósito: un puesto no se borra (RN15) y `app_floreria` no tiene `DELETE` sobre `puesto` |

## Módulos de asistencia y pagos

### Reglas por módulo

| Módulo | Clase | Reglas |
| --- | --- | --- |
| Checador | `ChecadorNegocio` | Compara la clave contra el hash (regla 6 del contrato); el mismo mensaje para empleado inexistente, inactivo o clave incorrecta; hora del servidor (V05); no domingo; si la página indica la checada esperada y no es la que sigue, avisa cuál sigue; los errores 1644 de `sp_registrar_checada` se muestran tal cual |
| Ausencias | `AusenciaNegocio` | Empleado válido; tipo Justificada / Injustificada / Vacaciones (V08); no domingo; no antes del ingreso; un registro por día; no en un día con checadas (RN17, V16); no se modifica ni elimina si la semana está cerrada (V09) |
| Correcciones de checadas | `AsistenciaNegocio` | Siempre `metodo = 'Manual'` y `modificado_por` = usuario activo (regla 5, V07); entrada obligatoria; horas en orden y del mismo día; no días futuros ni domingos; no en un día con ausencia (RN17); no se corrige ni elimina si la semana está cerrada; la auditoría la escribe el trigger |
| Semanas | `SemanaNegocio` | Inicia en lunes, termina en sábado; no se repite |
| Pagos | `PagoNegocio` | El cálculo lo hace sólo `sp_generar_pago` (regla 1); sin `MySqlTransaction` (regla 4); semana, empleado y usuario válidos; no se cierra si hay días sin checada ni ausencia (V10); un pago cerrado no se regenera; las observaciones se permiten aunque esté cerrado |

### Métodos expuestos a la capa Web

| Clase | Métodos |
| --- | --- |
| `ChecadorNegocio` | `Registrar(idEmpleado, clave, esperada?)`, `SiguienteChecada(idEmpleado)` |
| `AusenciaNegocio` | `Listar(idEmpleado?, desde?, hasta?)`, `BuscarPorId`, `Agregar`, `Modificar`, `Eliminar`, `Tipos` |
| `AsistenciaNegocio` | `Listar(idEmpleado?, desde?, hasta?)`, `BuscarPorId`, `Agregar`, `Modificar`, `Eliminar`, `ReporteDiario(desde, hasta, idEmpleado?)` |
| `SemanaNegocio` | `Listar`, `BuscarPorId`, `Agregar(fechaInicio)`, `LunesDe(fecha)` |
| `PagoNegocio` | `ListarResumen(idSemana?, idEmpleado?, estado?)`, `BuscarResumen`, `Generar`, `GenerarSemana`, `ObtenerTicket`, `ObtenerTicketPorPago`, `Cerrar`, `AgregarObservacion`, `DiasSinChecada` |
| `UsuarioNegocio` | `ListarActivos` |

### Traducción de errores de MySQL (A08)

| Número | Mensaje al usuario |
| --- | --- |
| 1644 | El texto del `SIGNAL` tal cual (por ejemplo, "No se checa los domingos: no hay horario.") |
| 1062 | Ya existe un registro con ese dato. |
| 1451 | No se puede borrar: tiene registros relacionados. Desactívelo en lugar de borrarlo. |
| 1452 | El empleado, puesto, semana o usuario indicado no existe. |
| 3819 | Los datos no cumplen las reglas de la base de datos (revise montos, fechas y el orden de las horas). |

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
- PuestoDatos, AusenciaDatos, RegistroAsistenciaDatos, SemanaDatos, PagoDatos y
  UsuarioDatos: Sí. Todos los valores van como `@parametro`; los filtros opcionales usan
  `(@param IS NULL OR columna = @param)`.
- ChecadorDatos y PagoDatos llaman a los procedimientos con
  `CommandType.StoredProcedure` y parámetros `p_*`.
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

Ejecutada el 2026-11-22 con la cadena de User Secrets de `Bellissimo.Web`, conectando como
`app_floreria` (mínimo privilegio), sobre la base reconstruida con `Sql/01`–`05`. Deja la base
como estaba (el empleado temporal de prueba se crea y se elimina).

**Resultado: 26 verificaciones OK, 0 fallas (12/12 casos aprobados).**

| No. | Caso | Esperado | Resultado |
| ---: | --- | --- | :---: |
| 1 | `ProbarConexion` | Cuenta empleados | APROBADO |
| 2 | `PuestoNegocio.Listar` | Recepcionista, Repartidor, Florista | APROBADO |
| 3 | `Listar` | Empleados con `NombrePuesto` | APROBADO |
| 4 | `Buscar("Ana")`, `Buscar("Florista")`, `Buscar("Ana Torres")` | Filtra por nombre, puesto y nombre completo | APROBADO |
| 5 | `Buscar("' OR 1=1 -- ")` | 0 resultados (sin inyección) | APROBADO |
| 6 | `BuscarPorId` existente / `99999` / `0` | Empleado / `null` / `null` | APROBADO |
| 7 | `Agregar` con nombre vacío, sueldo 0, puesto 999, teléfono vacío | `ArgumentException` con el mensaje de la regla | APROBADO |
| 8 | `Agregar` válido | Id nuevo, `fecha_ingreso` = hoy, `activo = 1` | APROBADO |
| 9 | `Modificar` (baja lógica `Activo = false`, cambio de puesto) | `true` y cambios guardados | APROBADO |
| 10 | `Eliminar` a María (tiene checadas en `Sql/02_datos.sql`) | `ReglaNegocioException`; sigue existiendo | APROBADO |
| 11 | `Eliminar(0)` / `Eliminar(99999)` | `ArgumentException` / `false` | APROBADO |
| 12 | `Eliminar` del empleado temporal | `true`; ya no existe | APROBADO |

### Prueba de integración de todos los módulos — 2026-11-22

Misma conexión (`app_floreria`). Esta prueba **sí modifica la base** a propósito (cierra un
pago, que ya no se puede deshacer); al terminar, la base se reconstruyó con `Sql/01`–`05`.
Se usó la semana del 16 al 21 de noviembre con la empleada Carla (sin historial previo).
Para las checadas de días hábiles se fijó la hora de prueba; con la hora real (domingo) se
comprobó el rechazo de domingo.

**Resultado: 87 verificaciones OK, 0 fallas.**

| Área | Casos verificados | Resultado |
| --- | --- | :---: |
| Personal (regresión) | Listar, búsqueda por puesto, inyección, validación, alta/baja sin historial, bloqueo con historial | APROBADO |
| Errores de MySQL | 1062, 1451, 1452 y 3819 llegan como `BaseDatosException`; Negocios los traduce y deja 1644 tal cual | APROBADO |
| Puestos | Duplicado (sin importar espacios), vacío, alta, edición, nombre repetido al editar, inexistente | APROBADO |
| Checador | Sin clave, clave inválida, hash guardado (no texto plano), `TieneClave`, clave incorrecta, empleado inexistente, domingo, las 4 checadas en orden, checada esperada distinta, quinta checada (Negocios y `sp`), hora anterior (`sp`) | APROBADO |
| Ausencias | Listar y filtros, rango invertido, alta con `Trim`, duplicado, domingo, tipo inválido, antes del ingreso, día con checadas (RN17), edición, baja, inexistente | APROBADO |
| Correcciones de checadas | Sin quién corrige, horas fuera de orden, hora de otro día, regreso sin salida, día futuro, día con ausencia, día repetido, alta `Manual`, edición, **auditoría escrita por el trigger**, baja, reporte `v_asistencia_diaria` (retardo de 10 min) | APROBADO |
| Semanas y pagos | Semana en martes, repetida, alta; resumen y filtros; estado inválido; ticket inexistente; semana/usuario inválidos; días sin checada; generar y ticket con renglones; **no cierra con días sin checada**; cierre; doble cierre; regenerar cerrado (1644); observación en pago cerrado; ausencias y correcciones bloqueadas en semana cerrada; checar en semana cerrada (trigger); `GenerarSemana` con omitidos | APROBADO |

### Prueba Web con base de datos — 2026-11-22

| Página | Resultado |
| --- | --- |
| `/Personal` | 5 empleados de MySQL, sin alerta de error |
| `/Personal?Buscar=Florista` | Sólo Ana Torres y Luis Ramírez (puesto Florista) |
| `/Personal/Nuevo` | `<select>` con Florista, Recepcionista y Repartidor desde la tabla `puesto` |
| `/Personal/Detalle/1` | María López: Recepcionista, $3,000.00 |
| `/Personal/Eliminar/1` (POST) | HTTP 200, mensaje de `ReglaNegocioException`; María no se elimina |

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
- Capas de Datos y Negocios de Checador, Ausencias, Correcciones de checadas, Puestos,
  Semanas, Pagos y Usuarios; entidades `Ausencia`, `RegistroAsistencia`,
  `AsistenciaDiaria`, `ResultadoChecada`, `Semana`, `ResumenPago`, `Ticket`,
  `DiaSinChecada` y `Usuario`. A `Personal` se le agregó `TieneClave` y a `Puesto` sus
  Data Annotations (sólo se agregaron propiedades; nada de lo existente cambió).
- Traducción de los errores 1644, 1062, 1451, 1452 y 3819 (`BaseDatosException` →
  `ReglaNegocioException`).

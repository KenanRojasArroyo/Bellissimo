# Contrato de la base de datos para el equipo de la aplicación

**Base:** `belissimo` · **Motor:** MySQL 8.0 · **Conector del curso:** `MySql.Data` (ADO.NET)
· **Usuario de la app:** `app_floreria` (`Sql/05_usuario_app.sql`)

Este documento dice qué puede llamar la página web, qué recibe y qué errores debe mostrar.
Los ejemplos de C# **se ejecutaron** el 2026-11-16 con MySql.Data 26.10.0 en .NET 10, conectados
con un usuario con los mismos permisos que `app_floreria`. Los 9 casos dieron el resultado
esperado.

## 0. Reglas obligatorias para la aplicación

1. **La app NO recalcula pagos.** El pago, los renglones del ticket y los totales los calcula y
   guarda **solo** `sp_generar_pago`. La app los lee con `sp_ticket` o con las vistas.
2. **La app NO escribe la auditoría.** `auditoria_registro` la llena el trigger
   `trg_registro_au_auditoria` cuando se corrige una checada. Si la app también insertara ahí,
   cada corrección quedaría duplicada; además, el usuario de la app no tiene permiso
   (error 1142).
3. **La app NO escribe en `pago_semanal` ni en `detalle_pago`.** Solo tiene `SELECT`. Para notas
   se usa `sp_agregar_observacion`, y para cerrar, `sp_cerrar_pago`.
4. **Llamar `sp_generar_pago` en modo autocommit**, nunca dentro de un `MySqlTransaction` de la
   app. El procedimiento abre su propia transacción, y en MySQL eso **confirma (COMMIT)** lo que
   la app tuviera pendiente (incidencia INC-04).
5. **Las checadas normales entran por `sp_registrar_checada`.** `INSERT`/`UPDATE` directos a
   `registro_asistencia` son solo para correcciones del encargado, y siempre con
   `metodo = 'Manual'` y `modificado_por = <id del usuario que corrige>`.
6. **La clave personal y la contraseña se comparan en la app** contra el hash
   (`empleado.clave_hash`, `usuario.contrasena`). La base nunca guarda texto plano.
7. **Errores de negocio:** todos los `SIGNAL` de la base llegan como
   `MySqlException` con `Number == 1644` y el texto en `ex.Message`. Mostrar ese texto al usuario.

## 1. Procedimientos almacenados

### `sp_registrar_checada(p_id_empleado INT, p_momento DATETIME, p_metodo VARCHAR(10))`

Registra la **siguiente** checada del día, en este orden: entrada → salida a comer → regreso →
salida.

| Parámetro | Significado |
| --------- | ----------- |
| `p_id_empleado` | Empleado que checa (debe existir y estar activo) |
| `p_momento` | Fecha y hora de la checada (usar la hora del servidor) |
| `p_metodo` | `'Clave'` o `'Huella'` |

**Devuelve 1 resultado:** `checada` (`ENTRADA` \| `SALIDA_COMIDA` \| `REGRESO_COMIDA` \| `SALIDA`),
`hora` (DATETIME).

**Errores 1644:**
- `El empleado no existe o esta inactivo.`
- `Metodo de checada no valido (use Clave o Huella).`
- `No se checa los domingos: no hay horario.`
- `La hora es anterior a la checada previa del dia.`
- `El empleado ya tiene las cuatro checadas de hoy.`
- `La semana de esa checada ya esta cerrada y no se puede modificar.` (trigger)
- `El empleado tiene una ausencia registrada ese dia: no puede tener checadas.` (trigger; el día ya tiene ausencia)

```csharp
using var cmd = new MySqlCommand("sp_registrar_checada", cn) { CommandType = CommandType.StoredProcedure };
cmd.Parameters.AddWithValue("p_id_empleado", idEmpleado);
cmd.Parameters.AddWithValue("p_momento", DateTime.Now);
cmd.Parameters.AddWithValue("p_metodo", "Clave");
try
{
    using var rd = cmd.ExecuteReader();
    rd.Read();
    string checada = rd.GetString("checada");      // "ENTRADA", "SALIDA_COMIDA", ...
    DateTime hora  = rd.GetDateTime("hora");
}
catch (MySqlException ex) when (ex.Number == 1644)
{
    mensaje = ex.Message;                           // ej. "No se checa los domingos: no hay horario."
}
```

### `sp_generar_pago(p_id_empleado INT, p_id_semana INT, p_id_usuario INT)`

Calcula el pago de la semana y lo guarda en `pago_semanal` + `detalle_pago`, dentro de una
transacción: si algo falla, deshace todo. Se puede volver a ejecutar mientras el pago esté en
**Borrador**; cada vez recalcula desde las checadas y ausencias actuales.

| Parámetro | Significado |
| --------- | ----------- |
| `p_id_empleado` | Empleado |
| `p_id_semana` | Semana (`semana.id_semana`) |
| `p_id_usuario` | Encargado que genera el pago (`usuario.id_usuario`; puede ser NULL) |

**No devuelve resultados.** Para ver el pago, usar `sp_ticket`.

**Errores 1644:** `La semana indicada no existe.` · `El empleado indicado no existe.` ·
`Faltan parametros de tolerancia o tiempo extra en configuracion.` ·
`Falta el parametro horas_semana_base en configuracion.` ·
`El pago de esta semana ya esta cerrado y no se puede regenerar.`
**Error 1452:** `p_id_usuario` no existe (se hace ROLLBACK y se relanza).

```csharp
// SIN MySqlTransaction alrededor (regla 4)
using var cmd = new MySqlCommand("sp_generar_pago", cn) { CommandType = CommandType.StoredProcedure };
cmd.Parameters.AddWithValue("p_id_empleado", idEmpleado);
cmd.Parameters.AddWithValue("p_id_semana", idSemana);
cmd.Parameters.AddWithValue("p_id_usuario", idUsuarioSesion);
cmd.ExecuteNonQuery();
```

### `sp_ticket(p_id_empleado INT, p_id_semana INT)`

Muestra el ticket **sin recalcular**: es exactamente lo que quedó guardado.

**Resultado 1 (resumen, 1 fila):** `empleado`, `puesto`, `fecha_inicio`, `fecha_fin`,
`sueldo_base`, `valor_hora`, `min_exceso_comida`, `min_retardo`, `min_salida_temprana`,
`min_falta`, `min_extra`, `descuento_total`, `monto_extra`, `salario_a_pagar`,
`estado` (`Borrador` \| `Cerrado`), `fecha_generacion`, `generado_por`, `observaciones`.

**Resultado 2 (renglones, 0..N filas):** `fecha` (NULL en conceptos semanales), `concepto`
(texto: `Comida de más`, `Retardo`, `Descuento por retardos acumulados`,
`Salida antes de tiempo`, `Falta injustificada`, `Tiempo extra`), `minutos`, `monto`
(negativo = descuento, 0 = informativo, positivo = pago). Una semana perfecta no tiene renglones.

**Error 1644:** `Ese pago todavia no se ha generado.`

```csharp
using var cmd = new MySqlCommand("sp_ticket", cn) { CommandType = CommandType.StoredProcedure };
cmd.Parameters.AddWithValue("p_id_empleado", idEmpleado);
cmd.Parameters.AddWithValue("p_id_semana", idSemana);
using var rd = cmd.ExecuteReader();
rd.Read();
decimal salario = rd.GetDecimal("salario_a_pagar");
string estado   = rd.GetString("estado");
rd.NextResult();                                    // segundo resultado: renglones
while (rd.Read())
{
    DateTime? fecha = rd.IsDBNull(rd.GetOrdinal("fecha")) ? null : rd.GetDateTime("fecha");
    string concepto = rd.GetString("concepto");
    int minutos     = rd.GetInt32("minutos");
    decimal monto   = rd.GetDecimal("monto");
}
```

### `sp_cerrar_pago(p_id_pago INT)`

Marca el pago como **Cerrado**. A partir de ese momento no se pueden cambiar las checadas, las
ausencias ni el ticket de esa semana y empleado, ni los montos del pago, y el pago no se puede
borrar. Solo se permiten observaciones.

**Errores 1644:** `El pago indicado no existe.` · `El pago ya estaba cerrado.`

### `sp_agregar_observacion(p_id_pago INT, p_texto VARCHAR(255))`

Guarda (o **reemplaza**) la nota del pago. Se permite aunque el pago esté cerrado.

**Error 1644:** `El pago indicado no existe.`

## 2. Funciones

| Función | Devuelve | Uso |
| ------- | -------- | --- |
| `fn_valor_hora(p_sueldo DECIMAL(10,2))` | `DECIMAL(10,4)` = sueldo ÷ 48 | `SELECT fn_valor_hora(@s)` → `3300` da `68.7500` |
| `fn_minutos_comida_extra(p_salida DATETIME, p_regreso DATETIME)` | `INT` minutos de comida por encima de 30 (0 si falta una checada) | Mostrar en pantalla |
| `fn_semana_cerrada(p_id_empleado INT, p_fecha DATE)` | `TINYINT` 1 si esa semana ya está cerrada para ese empleado | Deshabilitar botones de edición |

## 3. Vistas (solo lectura)

| Vista | Columnas | Para qué pantalla |
| ----- | -------- | ----------------- |
| `v_asistencia_diaria` | `id_registro, id_empleado, empleado, fecha, entrada, salida, horas_presentes, min_comida, min_comida_extra, min_retardo, min_salida_temprana, min_extra, metodo` | Reporte de asistencia |
| `v_resumen_pago_semanal` | `id_pago, id_semana, fecha_inicio, fecha_fin, id_empleado, empleado, puesto, sueldo_base, descuento_total, monto_extra, salario_final, estado` | Pantalla principal de pagos |
| `v_dias_sin_checada` | `id_semana, id_empleado, empleado, fecha, dia_semana, minutos_jornada, semana_cerrada` | Aviso antes de cerrar la semana: capturar la ausencia que corresponda |

```csharp
using var cmd = new MySqlCommand(
    "SELECT empleado, salario_final, estado FROM v_resumen_pago_semanal WHERE id_semana = @s ORDER BY id_empleado", cn);
cmd.Parameters.AddWithValue("@s", idSemana);       // SIEMPRE parámetros, nunca concatenar texto
```

## 4. Tablas que la app puede escribir directamente

| Tabla | Permisos de `app_floreria` | Notas |
| ----- | -------------------------- | ----- |
| `empleado` | SELECT, INSERT, UPDATE, DELETE | `activo` es `BOOLEAN` (se lee como `bool`). Baja = `activo = 0`; `DELETE` falla con 1451 si tiene historial. |
| `puesto`, `usuario` | SELECT, INSERT, UPDATE | `nombre` / `nombre_usuario` únicos (1062). |
| `ausencia` | SELECT, INSERT, UPDATE, DELETE | Un registro por empleado y día (1062). Bloqueada si la semana está cerrada (1644). **No se acepta en un día en que el empleado ya checó** (1644 `El empleado ya tiene checadas ese dia: ...`). |
| `registro_asistencia` | SELECT, INSERT, UPDATE, DELETE | Solo para correcciones; ver regla 5. **No se acepta en un día con ausencia** del empleado (1644). Para que alguien que tenía ausencia pueda checar, primero se borra la ausencia (si la semana sigue abierta). |
| `semana` | SELECT, INSERT | `fecha_inicio` (lunes) única; `fecha_fin` > `fecha_inicio`. |
| `horario_general`, `configuracion` | SELECT, UPDATE | Cambiar `configuracion` cambia los pagos que se generen después (los cerrados no cambian). |
| `pago_semanal`, `detalle_pago`, `auditoria_registro` | **Solo SELECT** | Ver reglas 1–3. |

## 5. Códigos de error que la app debe manejar

| Número | Significado | Qué mostrar |
| ------ | ----------- | ----------- |
| 1644 | Regla de negocio (`SIGNAL`) | `ex.Message` tal cual |
| 1062 | Valor duplicado en una llave única | "Ya existe un registro con ese dato" (ej. dos checadas del mismo día) |
| 1451 | Se intenta borrar algo con historial | "No se puede borrar: tiene registros relacionados; desactívelo" |
| 1452 | Referencia a algo que no existe (FK) | "El empleado/puesto/usuario indicado no existe" |
| 3819 | Falla una restricción `CHECK` | Ej. sueldo ≤ 0, checadas fuera de orden |
| 1142 | Permiso denegado | Error de programación: la app intentó escribir donde no debe |

## 6. Diferencias detectadas con el código de la app (revisar con el equipo)

Revisión de solo lectura del 2026-11-16 de `origin/main` y `origin/frontend`:

| Tema | App | Base de datos (regla acordada) |
| ---- | --- | ------------------------------ |
| Sueldo | `Personal.SueldoBase` con etiqueta **"Sueldo base / hr"** y `Range(0, …)` | `empleado.sueldo_base_semanal`: sueldo **semanal** ($3,000–$3,300), **> 0** (`ck_empleado_sueldo`) |
| Puestos | `enum TipoCargo { EncargadoGeneral, EncargadoFlorista, Florista, Repartidor }` | Tabla `puesto`: Recepcionista, Repartidor, Florista. Sugerencia: leer los puestos de la tabla en lugar de un `enum` |
| Correo | `Cliente.Correo` en `main` | No existe columna de correo |
| Conector | Ningún proyecto referencia todavía `MySql.Data` | Agregar `MySql.Data` al proyecto `Bellissimo.Datos` |

## 7. Registro de cambios de esquema

| Fecha | Cambio | ¿Afecta a la app? |
| ----- | ------ | ----------------- |
| 2026-11-16 | `empleado.activo`, `usuario.activo` y `pago_semanal.cerrado` se declaran `BOOLEAN` en lugar de `TINYINT(1)` (MySQL 8 marcaba la advertencia 1681). | **No.** MySQL guarda `BOOLEAN` como `tinyint(1)`; MySql.Data lo lee como `bool` (verificado). |
| 2026-11-16 | Índices nuevos: `idx_registro_fecha`, `idx_ausencia_fecha`, `idx_detalle_pago_concepto`, `idx_empleado_nombre`, `idx_auditoria_registro`. | **No.** Solo aceleran consultas. |
| 2026-11-16 | `trg_pago_bu_cerrado` ahora rechaza cambiar **cualquier** columna de un pago cerrado excepto `observaciones`. Nuevo `trg_pago_bd_cerrado`: un pago cerrado no se puede borrar. | **Sí.** Si la app intenta editar o borrar un pago cerrado recibirá el error 1644 (`El pago esta cerrado: ...`). Para notas, usar `sp_agregar_observacion`. |
| 2026-11-16 | Nuevos `trg_ausencia_bi/bu/bd_cerrado`: no se pueden agregar, cambiar ni borrar ausencias de una semana con pago cerrado. | **Sí.** Error 1644 `La semana de esa ausencia ya esta cerrada y no se puede modificar.` La pantalla de ausencias debe mostrar ese mensaje. |
| 2026-11-16 | Nueva vista `v_dias_sin_checada`: días laborales sin checada ni ausencia. | **Sí (nueva pantalla sugerida).** Mostrarla al encargado antes de generar o cerrar pagos, para que capture la ausencia que corresponda. |
| 2026-11-16 | Cada tabla y columna tiene `COMMENT` con su significado (base del diccionario de datos). | **No.** Solo metadato. |
| 2026-11-16 | `trg_registro_au_auditoria` guarda en `usuario_bd` la cuenta conectada (`USER()`) en vez del definer (INC-05). | **No** cambia la interfaz; ahora la auditoría muestra `app_floreria@localhost`. |
| 2026-11-16 | Nuevo `Sql/05_usuario_app.sql` (usuario `app_floreria` de mínimo privilegio). | **Sí.** La app debe conectarse con ese usuario, no con root (ver `Sql/conexion_plantilla.md`). |
| 2026-11-16 | Nuevos `trg_registro_bi/bu_ausencia` y `trg_ausencia_bi/bu_checada` (PEN-01): un día es checada **o** ausencia, nunca las dos. Total: 16 triggers. | **Sí.** El checador y la pantalla de ausencias pueden recibir 1644 con `El empleado tiene una ausencia registrada ese dia: ...` o `El empleado ya tiene checadas ese dia: ...`; mostrar el mensaje tal cual. |

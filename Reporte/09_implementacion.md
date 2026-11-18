# 10. Implementación en MySQL 8.0

**Entorno:** MySQL Community Server 8.0.46 (Windows 11), InnoDB, `utf8mb4` /
`utf8mb4_unicode_ci`, MySQL Workbench 8.0. Scripts en `Sql/`, ejecutados en orden
`01 → 02 → 03 → 04` (y `05` para el usuario de la app). Todo se reconstruye desde cero porque
`01_esquema.sql` empieza con `DROP DATABASE IF EXISTS`.

| Objeto | Cantidad | Dónde |
| ------ | -------- | ----- |
| Tablas | 11 | `01_esquema.sql` |
| Restricciones `CHECK` con nombre | 7 | `01_esquema.sql` |
| Llaves foráneas con nombre | 8 (todas `ON UPDATE CASCADE` / `ON DELETE RESTRICT`) | `01_esquema.sql` |
| Índices | 27 (PK, `UNIQUE`, FK y 5 `idx_` de búsqueda) | `01_esquema.sql` |
| Funciones | 3 | `03_rutinas.sql` |
| Procedimientos | 5 | `03_rutinas.sql` |
| Triggers | 16 | `03_rutinas.sql` |
| Vistas | 3 | `04_consultas_vistas.sql` |
| Consultas documentadas | 10 | `04_consultas_vistas.sql` |

## 10.1 Tablas y restricciones

Cada tabla y columna tiene un `COMMENT` con su significado; el diccionario (sección 6) se genera
de ahí. Restricciones `CHECK`:

| Restricción | Regla |
| ----------- | ----- |
| `ck_empleado_sueldo` | `sueldo_base_semanal > 0` |
| `ck_horario_dia` | `dia_semana BETWEEN 1 AND 6` (lunes a sábado) |
| `ck_horario_horas` | `hora_salida > hora_entrada` |
| `ck_registro_fecha` | `entrada IS NULL OR DATE(entrada) = fecha` |
| `ck_registro_orden` | entrada ≤ salida a comer ≤ regreso ≤ salida |
| `ck_semana_fechas` | `fecha_fin > fecha_inicio` |
| `ck_pago_montos` | sueldo > 0; descuento, extra y salario ≥ 0 |

Llaves únicas que implementan claves candidatas: `uq_registro_dia`, `uq_ausencia_dia`,
`uq_pago_empleado_semana`, más `UNIQUE` en `puesto.nombre`, `usuario.nombre_usuario`,
`empleado.id_huella` y `semana.fecha_inicio`.

## 10.2 Integridad de entidad, referencial y de dominio

**Integridad de entidad.** Cada fila debe poder identificarse sin ambigüedad.
- Las 11 tablas tienen **PRIMARY KEY** (no nula y única por definición). Nueve usan una llave
  sustituta `INT AUTO_INCREMENT`; `horario_general` (`dia_semana`) y `configuracion` (`parametro`)
  usan una llave natural.
- Las **llaves naturales** se protegen con `UNIQUE`, incluidas tres **compuestas**:
  `uq_registro_dia (id_empleado, fecha)`, `uq_ausencia_dia (id_empleado, fecha)` y
  `uq_pago_empleado_semana (id_empleado, id_semana)`. Así una PK sustituta no permite duplicar el
  mismo hecho (evidencia: P15b, `ERROR 1062 … uq_registro_dia`).
- *Justificación:* el pago se calcula por empleado y día. Dos registros del mismo día duplicarían
  descuentos o extra.

**Integridad referencial.** Toda referencia apunta a una fila que existe.
- 8 **FOREIGN KEY** con nombre (`fk_…`), todas con `ON UPDATE CASCADE` y `ON DELETE RESTRICT`.
- `RESTRICT`: no se puede borrar un empleado, puesto, semana o pago que tenga historial, porque
  se perdería la justificación de pagos ya hechos (P05, `ERROR 1451`). La baja normal es `activo = 0`.
- `CASCADE` al actualizar: si cambiara una llave, las referencias la siguen. Con llaves sustitutas
  casi no ocurre.
- FK **opcionales** (admiten NULL) donde el negocio lo permite: `registro_asistencia.modificado_por`
  (solo en correcciones) y `pago_semanal.id_usuario`.
- Excepción **justificada:** `auditoria_registro` no tiene FK a propósito, para que la bitácora
  sobreviva aunque el registro auditado se borre.
- Evidencia: P15a y P20 (`ERROR 1452`: no se acepta un empleado o un usuario inexistente).

**Integridad de dominio.** Cada valor pertenece a su conjunto válido.
- **Tipos adecuados:** `DECIMAL(10,2)` para dinero (sin errores de redondeo binario), `DATE`,
  `TIME` y `DATETIME` para el tiempo, `BOOLEAN` para banderas, y `ENUM` para listas cerradas
  (`rol`, `metodo`, `tipo` de ausencia, `concepto` del ticket).
- **NOT NULL** en todo lo obligatorio; los opcionales están justificados en §4.2.
- **DEFAULT:** `activo = 1`, `cerrado = 0`, `metodo = 'Clave'`, `rol = 'Encargado'`, minutos y
  montos en 0, `fecha_generacion` y `fecha_cambio` con `CURRENT_TIMESTAMP`.
- **CHECK** (7, ver §10.1): sueldo > 0, días 1–6, salida > entrada, fecha de la checada, orden de
  las 4 checadas, fin de semana > inicio y montos no negativos (P04, P15c y P15d, `ERROR 3819`).
- **Reglas de dominio que dependen de otras filas** (no caben en un `CHECK`): se hacen cumplir
  con triggers (semana cerrada; checada o ausencia, no ambas) y con `sp_registrar_checada`
  (domingo, hora anterior, máximo 4 checadas).

## 10.3 Funciones

| Función | Qué calcula |
| ------- | ----------- |
| `fn_valor_hora(sueldo)` | sueldo ÷ `horas_semana_base` (48); error si falta el parámetro |
| `fn_minutos_comida_extra(salida, regreso)` | minutos de comida por encima de `comida_min` (30) |
| `fn_semana_cerrada(empleado, fecha)` | 1 si el pago de esa semana ya está cerrado (la usan los triggers) |

## 10.4 Procedimientos almacenados

| Procedimiento | Qué hace |
| ------------- | -------- |
| `sp_registrar_checada` | Decide cuál de las 4 checadas sigue y la guarda; valida empleado activo, método, domingo, orden y máximo 4 |
| `sp_generar_pago` | Calcula retardos, comida de más, salida temprana, faltas injustificadas, tiempo extra (×2 hasta 540 min y ×3 después, con `SUM() OVER`) y retardos acumulados (si el dueño define la regla); guarda pago y renglones **en una transacción** |
| `sp_ticket` | Devuelve el resumen y los renglones guardados (no recalcula) |
| `sp_cerrar_pago` | Congela el pago |
| `sp_agregar_observacion` | Nota del encargado, permitida aun con el pago cerrado |

## 10.5 Transacciones

`sp_generar_pago` ejecuta `START TRANSACTION … COMMIT` con
`DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END`. Si algo falla a la mitad,
por ejemplo un usuario inexistente que viola `fk_pago_usuario`, se deshace todo, incluso el
borrado de renglones que ya se había hecho (prueba **P20**: error 1452 y el pago 8 queda igual).
Las pruebas **P11** (COMMIT), **P12** (ROLLBACK manual) y **P19** (ROLLBACK con vista) completan
la demostración. Limitación documentada (**INC-04**): el `START TRANSACTION` interno confirma lo
que el llamador tuviera abierto, por eso la app debe llamar el procedimiento en autocommit.

## 10.6 Triggers

Los 16 triggers documentan en `Sql/03_rutinas.sql` su evento, condición y efecto. Resumen:

| Trigger | Evento | Condición | Efecto |
| ------- | ------ | --------- | ------ |
| `trg_registro_bi_cerrado` | BEFORE INSERT en `registro_asistencia` | `fn_semana_cerrada(NEW.id_empleado, NEW.fecha) = 1` | Rechaza la checada (1644) |
| `trg_registro_bu_cerrado` | BEFORE UPDATE en `registro_asistencia` | La semana de la fila original **o** la de la nueva está cerrada | Rechaza el cambio (1644) |
| `trg_registro_bd_cerrado` | BEFORE DELETE en `registro_asistencia` | `fn_semana_cerrada(OLD.id_empleado, OLD.fecha) = 1` | Rechaza el borrado (1644) |
| `trg_registro_au_auditoria` | AFTER UPDATE en `registro_asistencia` | Cambió alguna de las 4 horas que **ya tenía valor** (llenar una vacía no se audita) | Inserta en `auditoria_registro` el antes, el después, `modificado_por` y `USER()` |
| `trg_ausencia_bi_cerrado` | BEFORE INSERT en `ausencia` | `fn_semana_cerrada(NEW.id_empleado, NEW.fecha) = 1` | Rechaza la ausencia (1644) |
| `trg_ausencia_bu_cerrado` | BEFORE UPDATE en `ausencia` | La semana original **o** la nueva está cerrada | Rechaza el cambio (1644) |
| `trg_ausencia_bd_cerrado` | BEFORE DELETE en `ausencia` | `fn_semana_cerrada(OLD.id_empleado, OLD.fecha) = 1` | Rechaza el borrado (1644) |
| `trg_detalle_bi_cerrado` | BEFORE INSERT en `detalle_pago` | El pago `NEW.id_pago` tiene `cerrado = 1` | Rechaza el renglón (1644) |
| `trg_detalle_bu_cerrado` | BEFORE UPDATE en `detalle_pago` | El pago `OLD.id_pago` tiene `cerrado = 1` | Rechaza el cambio (1644) |
| `trg_detalle_bd_cerrado` | BEFORE DELETE en `detalle_pago` | El pago `OLD.id_pago` tiene `cerrado = 1` | Rechaza el borrado (1644) |
| `trg_pago_bu_cerrado` | BEFORE UPDATE en `pago_semanal` | `OLD.cerrado = 1` y cambia cualquier columna **excepto** `observaciones` | Rechaza el cambio (1644) |
| `trg_pago_bd_cerrado` | BEFORE DELETE en `pago_semanal` | `OLD.cerrado = 1` | Rechaza el borrado (1644) |
| `trg_registro_bi_ausencia` | BEFORE INSERT en `registro_asistencia` | Existe una `ausencia` del mismo empleado en `NEW.fecha` | Rechaza la checada (1644) |
| `trg_registro_bu_ausencia` | BEFORE UPDATE en `registro_asistencia` | Cambia el empleado o la fecha **y** el nuevo día tiene ausencia | Rechaza el cambio (1644) |
| `trg_ausencia_bi_checada` | BEFORE INSERT en `ausencia` | Existe un `registro_asistencia` del mismo empleado en `NEW.fecha` | Rechaza la ausencia (1644) |
| `trg_ausencia_bu_checada` | BEFORE UPDATE en `ausencia` | Cambia el empleado o la fecha **y** el nuevo día tiene checadas | Rechaza el cambio (1644) |

**Sin doble ejecución:** los triggers solo validan y auditan; los cálculos los hacen únicamente
los procedimientos. La aplicación no recalcula pagos ni escribe la auditoría, y además no tiene
permiso para hacerlo (`Sql/CONTRATO_BD.md`, regla 0).

## 10.7 Seguridad y respaldo

- **Usuario de la app `app_floreria`** (`Sql/05_usuario_app.sql`): `EXECUTE` en rutinas,
  `SELECT` en vistas, DML solo donde el CRUD lo necesita y **solo lectura** en pagos y
  auditoría. No tiene `DROP`, `ALTER` ni `CREATE`, ni acceso a otras bases. Probado: 17/17 casos.
- **Credenciales:** ninguna contraseña en el repositorio. La app usa user-secrets o variables de
  entorno (`Sql/conexion_plantilla.md`). Contraseñas y claves se guardan como hash.
- **Respaldo:** `Sql/respaldo_restauracion.ps1` con `mysqldump --single-transaction --routines
  --triggers --events`. La restauración en una base aparte dio el mismo `CHECKSUM` en las 11
  tablas. Los respaldos no se suben a GitHub (`Sql/.gitignore`). Se recomienda una copia fuera de
  la computadora y probar la restauración periódicamente.

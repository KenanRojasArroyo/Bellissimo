# 9. Implementación en MySQL 8.0

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

## 9.1 Tablas y restricciones

Cada tabla y columna tiene un `COMMENT` con su significado; el diccionario (sección 5) se genera
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

## 9.2 Funciones

| Función | Qué calcula |
| ------- | ----------- |
| `fn_valor_hora(sueldo)` | sueldo ÷ `horas_semana_base` (48); error si falta el parámetro |
| `fn_minutos_comida_extra(salida, regreso)` | minutos de comida por encima de `comida_min` (30) |
| `fn_semana_cerrada(empleado, fecha)` | 1 si el pago de esa semana ya está cerrado (la usan los triggers) |

## 9.3 Procedimientos almacenados

| Procedimiento | Qué hace |
| ------------- | -------- |
| `sp_registrar_checada` | Decide cuál de las 4 checadas sigue y la guarda; valida empleado activo, método, domingo, orden y máximo 4 |
| `sp_generar_pago` | Calcula retardos, comida de más, salida temprana, faltas injustificadas, tiempo extra (×2 hasta 540 min y ×3 después, con `SUM() OVER`) y retardos acumulados (si el dueño define la regla); guarda pago y renglones **en una transacción** |
| `sp_ticket` | Devuelve el resumen y los renglones guardados (no recalcula) |
| `sp_cerrar_pago` | Congela el pago |
| `sp_agregar_observacion` | Nota del encargado, permitida aun con el pago cerrado |

## 9.4 Transacciones

`sp_generar_pago` ejecuta `START TRANSACTION … COMMIT` con
`DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END`. Si algo falla a la mitad,
por ejemplo un usuario inexistente que viola `fk_pago_usuario`, se deshace todo, incluso el
borrado de renglones que ya se había hecho (prueba **P20**: error 1452 y el pago 8 queda igual).
Las pruebas **P11** (COMMIT), **P12** (ROLLBACK manual) y **P19** (ROLLBACK con vista) completan
la demostración. Limitación documentada (**INC-04**): el `START TRANSACTION` interno confirma lo
que el llamador tuviera abierto, por eso la app debe llamar el procedimiento en autocommit.

## 9.5 Triggers

| Trigger | Evento | Efecto |
| ------- | ------ | ------ |
| `trg_registro_bi/bu/bd_cerrado` | BEFORE INSERT/UPDATE/DELETE en `registro_asistencia` | Rechaza cambios a checadas de una semana cerrada |
| `trg_registro_au_auditoria` | AFTER UPDATE en `registro_asistencia` | Si se cambió una hora que ya tenía valor, guarda el antes, el después, el usuario de la app y la cuenta de MySQL (`USER()`) |
| `trg_ausencia_bi/bu/bd_cerrado` | BEFORE INSERT/UPDATE/DELETE en `ausencia` | Rechaza cambios a ausencias de una semana cerrada |
| `trg_detalle_bi/bu/bd_cerrado` | BEFORE INSERT/UPDATE/DELETE en `detalle_pago` | Congela el ticket de un pago cerrado |
| `trg_pago_bu_cerrado` | BEFORE UPDATE en `pago_semanal` | En un pago cerrado solo permite cambiar `observaciones` |
| `trg_pago_bd_cerrado` | BEFORE DELETE en `pago_semanal` | Un pago cerrado no se puede borrar |
| `trg_registro_bi/bu_ausencia` | BEFORE INSERT/UPDATE en `registro_asistencia` | Rechaza una checada en un día con ausencia del mismo empleado (también al cambiar empleado o fecha) |
| `trg_ausencia_bi/bu_checada` | BEFORE INSERT/UPDATE en `ausencia` | Rechaza una ausencia en un día en que el empleado ya checó |

**Sin doble ejecución:** los triggers solo validan y auditan; los cálculos los hacen únicamente
los procedimientos. La aplicación no recalcula pagos ni escribe la auditoría, y además no tiene
permiso para hacerlo (`Sql/CONTRATO_BD.md`, regla 0).

## 9.6 Seguridad y respaldo

- **Usuario de la app `app_floreria`** (`Sql/05_usuario_app.sql`): `EXECUTE` en rutinas,
  `SELECT` en vistas, DML solo donde el CRUD lo necesita y **solo lectura** en pagos y
  auditoría. No tiene `DROP`, `ALTER` ni `CREATE`, ni acceso a otras bases. Probado: 17/17 casos.
- **Credenciales:** ninguna contraseña en el repositorio. La app usa user-secrets o variables de
  entorno (`Sql/conexion_plantilla.md`). Contraseñas y claves se guardan como hash.
- **Respaldo:** `Sql/respaldo_restauracion.ps1` con `mysqldump --single-transaction --routines
  --triggers --events`. La restauración en una base aparte dio el mismo `CHECKSUM` en las 11
  tablas. Los respaldos no se suben a GitHub (`Sql/.gitignore`). Se recomienda una copia fuera de
  la computadora y probar la restauración periódicamente.

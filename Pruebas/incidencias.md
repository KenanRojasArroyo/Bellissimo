# Incidencias reales de la base de datos

Solo se registran problemas que **ocurrieron de verdad** al ejecutar los scripts.
Entorno: MySQL Community Server **8.0.46** en Windows 11, cliente `mysql` 8.0, usuario `root`,
`log_bin = 1`, `log_bin_trust_function_creators = 0`, `sql_mode` por omisión de MySQL 8
(`ONLY_FULL_GROUP_BY, STRICT_TRANS_TABLES, NO_ZERO_IN_DATE, NO_ZERO_DATE, ERROR_FOR_DIVISION_BY_ZERO, NO_ENGINE_SUBSTITUTION`).

Antecedente: los scripts se escribieron y probaron primero en MariaDB 10.11. La primera
ejecución en MySQL 8 real fue el 2026-11-12 (base `belissimo`): 01–04 terminaron sin errores.

| ID | Fecha | Dónde | Qué pasó (mensaje exacto) | Causa | Corrección | Verificación |
| -- | ----- | ----- | ------------------------- | ----- | ---------- | ------------ |
| INC-01 | 2026-11-16 | `01_esquema.sql`, `CREATE TABLE` de `empleado`, `usuario` y `pago_semanal` | Cada uno respondió `Query OK, 0 rows affected, 1 warning`. `SHOW WARNINGS`: `Warning 1681 Integer display width is deprecated and will be removed in a future release.` | MySQL 8.0.17+ marca como obsoleto el ancho de despliegue `TINYINT(1)`; MariaDB no lo avisa. | Las columnas `activo` (2) y `cerrado` se declaran `BOOLEAN` (sinónimo que MySQL guarda como `tinyint(1)` sin advertencia). | Reconstrucción completa sin advertencias; regresión 22/22 OK. |
| INC-02 | 2026-11-16 | Trigger `trg_pago_bu_cerrado` | Con el pago 1 **cerrado**, `UPDATE pago_semanal SET min_retardo = 999, id_empleado = 5 WHERE id_pago = 1;` **se aceptó**: el pago cerrado de María quedó a nombre de Carla con 999 min de retardo. | El trigger solo comparaba 5 columnas (`sueldo_base_usado`, `valor_hora`, `descuento_total`, `monto_extra`, `salario_final`) y `cerrado`. | El trigger compara todas las columnas salvo `observaciones`. | Mismo `UPDATE` → `ERROR 1644 (45000): El pago esta cerrado: solo se pueden agregar observaciones.` |
| INC-03 | 2026-11-16 | Tabla `pago_semanal` (sin trigger de borrado) | Con el pago 3 (Ana, semana 1, sin renglones de ticket) **cerrado**, `DELETE FROM pago_semanal WHERE id_pago = 3;` **borró 1 fila**. | `fk_detalle_pago` (ON DELETE RESTRICT) solo protege pagos que tienen renglones; un pago perfecto no tiene. | Nuevo trigger `trg_pago_bd_cerrado`. | Mismo `DELETE` → `ERROR 1644 (45000): El pago esta cerrado: no se puede eliminar.` |
| INC-04 | 2026-11-16 | `sp_generar_pago` llamado dentro de otra transacción | `START TRANSACTION; UPDATE empleado SET telefono='111' WHERE id_empleado=4; CALL sp_generar_pago(4,2,2); ROLLBACK;` → el teléfono **quedó en '111'**. Sin el `CALL`, el mismo `ROLLBACK` sí deshace el cambio. | El procedimiento ejecuta su propio `START TRANSACTION`, y en MySQL eso hace un **COMMIT implícito** de la transacción abierta por quien llama. | No se cambia el SQL: MySQL no tiene una variable confiable para detectarlo (`SELECT @@in_transaction` → `ERROR 1193 (HY000): Unknown system variable 'in_transaction'`). Se documenta como **regla obligatoria** en `Sql/CONTRATO_BD.md`: la app llama `sp_generar_pago` en modo autocommit, nunca dentro de su propia transacción. | Reproducido dos veces en `belissimo_prueba`. |
| INC-05 | 2026-11-16 | Trigger `trg_registro_au_auditoria` | Conectado como el usuario de la app (`app_floreria_prueba`), una corrección de checada dejó en `auditoria_registro` el valor `usuario_bd = root@localhost`. | Dentro de un trigger, `CURRENT_USER()` devuelve la cuenta **DEFINER** del trigger (root), no la cuenta que se conectó. | Se usa `USER()`, que devuelve la cuenta de la sesión. | Misma corrección → `usuario_bd = app_floreria_prueba@localhost`; 17/17 casos de privilegios OK. |

## Hallazgos pendientes de decisión (no corregidos)

| ID | Fecha | Hallazgo (observado) | Por qué no se corrigió |
| -- | ----- | -------------------- | ---------------------- |
| PEN-01 | 2026-11-16 | Un empleado puede tener **checada y ausencia el mismo día**. Ana checó a las 08:55 el 2026-10-26 y aun así se aceptó `INSERT INTO ausencia (3, '2026-10-26', 'Injustificada')`; al regenerar su pago se le descontaron $531.25. | Es una regla de negocio: ¿se permite medio día de falta justificada (por ejemplo, se fue enfermo)? Si el dueño dice que no, se agrega un trigger que rechace la ausencia cuando hay checada (y al revés). |

## Observaciones que NO fueron errores

- **Error 1419 (permisos de funciones/triggers):** no ocurrió. Con `log_bin = 1` y
  `log_bin_trust_function_creators = 0`, `root` creó las 3 funciones y los triggers sin cambiar
  la variable. Solo haría falta con un usuario sin privilegio `SUPER`/`SET_USER_ID`.
- Al probar INC-02, el primer intento (`id_empleado = 4`) falló con
  `ERROR 1062 (23000): Duplicate entry '4-1' for key 'pago_semanal.uq_pago_empleado_semana'`.
  Eso fue la restricción `UNIQUE` funcionando bien, no el trigger.

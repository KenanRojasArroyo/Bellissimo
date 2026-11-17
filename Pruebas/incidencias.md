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

## Observaciones que NO fueron errores

- **Error 1419 (permisos de funciones/triggers):** no ocurrió. Con `log_bin = 1` y
  `log_bin_trust_function_creators = 0`, `root` creó las 3 funciones y los triggers sin cambiar
  la variable. Solo haría falta con un usuario sin privilegio `SUPER`/`SET_USER_ID`.
- Al probar INC-02, el primer intento (`id_empleado = 4`) falló con
  `ERROR 1062 (23000): Duplicate entry '4-1' for key 'pago_semanal.uq_pago_empleado_semana'`.
  Eso fue la restricción `UNIQUE` funcionando bien, no el trigger.

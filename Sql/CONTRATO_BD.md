# Contrato de la base de datos para el equipo de la aplicación

**Base:** `belissimo` · **Motor:** MySQL 8.0 · **Responsable:** DBA del equipo

> Documento en construcción: la referencia completa de procedimientos, funciones y vistas
> se agrega en la fase C. Esta primera sección registra los cambios de esquema que la
> aplicación debe conocer.

## Registro de cambios de esquema

| Fecha | Cambio | ¿Afecta a la app? |
| ----- | ------ | ----------------- |
| 2026-11-16 | `empleado.activo`, `usuario.activo` y `pago_semanal.cerrado` se declaran `BOOLEAN` en lugar de `TINYINT(1)` (MySQL 8 marcaba la advertencia 1681). | **No.** MySQL guarda `BOOLEAN` como `tinyint(1)`; MySql.Data lo sigue leyendo como `bool`. |
| 2026-11-16 | Índices nuevos: `idx_registro_fecha`, `idx_ausencia_fecha`, `idx_detalle_pago_concepto`, `idx_empleado_nombre`, `idx_auditoria_registro`. | **No.** Solo aceleran consultas. |
| 2026-11-16 | `trg_pago_bu_cerrado` ahora rechaza cambiar **cualquier** columna de un pago cerrado excepto `observaciones`. Nuevo `trg_pago_bd_cerrado`: un pago cerrado no se puede borrar. | **Sí.** Si la app intenta editar o borrar un pago cerrado recibirá el error 1644 (`El pago esta cerrado: ...`). Para notas usar `sp_agregar_observacion`. |
| 2026-11-16 | Nuevos `trg_ausencia_bi/bu/bd_cerrado`: no se pueden agregar, cambiar ni borrar ausencias de una semana con pago cerrado. | **Sí.** Error 1644 `La semana de esa ausencia ya esta cerrada y no se puede modificar.` La pantalla de ausencias debe mostrar ese mensaje. |
| 2026-11-16 | Nueva vista `v_dias_sin_checada`: días laborales sin checada ni ausencia. | **Sí (nueva pantalla sugerida).** Mostrarla al encargado antes de generar/cerrar pagos para que capture la ausencia que corresponda. |

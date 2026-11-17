# Presentación — apartados de base de datos (DBA)

Contenido sugerido para las diapositivas de la parte de BD. El resto de la presentación
(aplicación, capturas de la página) es **[A cargo de frontend/backend]**.

---

### Diapositiva BD-1 · El problema

- La asistencia se anota a mano y el pago semanal se calcula en papel.
- No hay ticket que justifique el pago ni rastro de correcciones.
- Objetivo: 4 checadas al día → pago semanal calculado, congelado y auditado.

### Diapositiva BD-2 · Reglas del negocio (las que cambian el dinero)

- Horario L–V 9:00–17:30, sábado 9:00–15:30; sueldo semanal ÷ 48 = valor de la hora.
- Tolerancia 5 min; comida 30 min pagada (exceso se descuenta); salida temprana se descuenta.
- Falta injustificada = jornada del día; justificada y vacaciones no descuentan.
- Extra: 540 min ×2 y después ×3 (configurable). Pago cerrado = congelado.

### Diapositiva BD-3 · Modelo (Crow's Foot)

- Imagen: `Diagramas/crows_foot.png`.
- 11 tablas, 8 FK. `pago_semanal` es la entidad **asociativa** empleado–semana.
- Checadas y ausencias son **débiles**: su llave natural es (empleado, fecha).

### Diapositiva BD-4 · Normalización

- Hoja manual (24 columnas de días + faltas en texto) → **no 1FN**.
- 1FN: una fila por empleado y día → 2FN y 3FN: puesto, horario, semana y ausencia aparte.
- `pago_semanal` rompe 3FN **a propósito**: es la foto histórica del pago, protegida por
  triggers.

### Diapositiva BD-5 · Lo que hace la base sola

- **5 procedimientos:** checada, generar pago (**transacción** con ROLLBACK), ticket, cerrar,
  observación.
- **12 triggers:** congelan la semana cerrada y **auditan** las correcciones.
- **3 vistas:** asistencia diaria, resumen de pagos, días sin checada.
- La app no recalcula ni audita: evita la doble ejecución.

### Diapositiva BD-6 · Pruebas y seguridad

- 32 casos en MySQL 8.0.46: todos pasan (válidos e inválidos con su error exacto).
- 5 incidencias **reales** encontradas y corregidas (por ejemplo, un pago cerrado se podía borrar).
- Usuario `app_floreria` de mínimo privilegio; respaldo con `mysqldump` probado.

### Diapositiva BD-7 · Resultado

- Semana 1: $11,868.73 · Semana 2: $14,562.50 (datos ficticios).
- Ticket de Juan semana 1: descuento $630.22, extra $137.50, pago $2,807.28.
- Pendientes con el dueño: regla de retardos, falta justificada, multiplicadores, checada y
  ausencia el mismo día.

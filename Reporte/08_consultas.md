# 8. Consultas SQL explicadas

Las 10 consultas están en `Sql/04_consultas_vistas.sql`. Los resultados son los que devolvió
**MySQL 8.0.46** el 2026-11-16, justo después de reconstruir con `01–04`. Las columnas de fecha y
hora de generación cambian con cada ejecución.

| # | Técnica | Pregunta del negocio | Resultado observado |
| - | ------- | -------------------- | ------------------- |
| 1 | `INNER JOIN` | ¿Qué empleados hay, en qué puesto y con qué sueldo? | 5 filas: Luis (Florista, 3150), Ana (Florista, 3000), María (Recepcionista, 3000), Carla (Recepcionista, 3000), Juan (Repartidor, 3300) |
| 2 | `JOIN` de 4 tablas + filtro | ¿Qué descuentos tuvo cada empleado en la semana 1 y de qué día? | 7 filas: María, 4 de comida de más (−8.33, −15.63, −22.92, −41.67); Juan, comida −11.46, falta −584.38 y salida temprana −34.38 |
| 3 | `LEFT JOIN` + `COUNT` | ¿Cuántos pagos tiene cada empleado, incluso quien no tiene ninguno? | María, Juan, Ana y Luis: 2 cada uno; **Carla: 0** (aparece gracias al `LEFT JOIN`) |
| 4 | Subconsulta escalar (`AVG`) | ¿Quién cobró más que el promedio en la semana 2? | Juan 5156.25 y Luis 3937.50 (promedio de la semana: 3640.63) |
| 5 | `NOT EXISTS` | ¿Qué empleados activos nunca han checado? | Carla Mendoza (ingresó el 2026-11-09) |
| 6 | `SUM` + `GROUP BY` + `HAVING` (sobre vista) | ¿Quién acumula retardos o comida de más? | María: 10 min de retardo y 85 de comida de más; Juan: 39 y 10 |
| 7 | `COUNT`, `SUM`, `AVG`, `MAX` + `GROUP BY` | ¿Cuánto se paga, descuenta y agrega cada semana? | Semana 1: 4 pagos, total 11868.73, promedio 2967.18, máximo 3150.00, descuentos 718.77, extra 137.50. Semana 2: 4 pagos, 14562.50, 3640.63, 5156.25, 531.25, 2643.75 |
| 8 | `COUNT` + `GROUP BY` + `HAVING` | ¿Qué puestos tienen incidencias con descuento? | Repartidor: 3 incidencias, 550 min, −630.22; Florista: 1, 510, −531.25; Recepcionista: 4, 85, −88.55 |
| 9 | Función de ventana `RANK() OVER (PARTITION BY …)` | ¿Quién fue más puntual cada semana? | Semana 1: Ana y Luis 1.º (0 min), Juan 3.º (79), María 4.ª (85). Semana 2: Juan, Ana y Luis 1.º (0), María 4.ª (10) |
| 10 | Tabla cruzada `SUM(CASE …)` | ¿Cómo queda cada ticket por concepto en una fila? | Ej.: Juan semana 1 → comida −11.46, salida −34.38, faltas −584.38, extra 137.50, **neto −492.72** (= −630.22 + 137.50) |

**Vistas** (también en `04`):

- `v_asistencia_diaria`: una fila por empleado y día, con horas, retardo, comida de más, salida
  temprana y extra. Base del reporte de asistencia.
- `v_resumen_pago_semanal`: el pago de cada empleado por semana con su estado. Con los datos de
  prueba devuelve los 8 pagos de la regresión.
- `v_dias_sin_checada`: días laborales sin checada ni ausencia. Con los datos de prueba devuelve
  **0 filas**, porque cada falta ya tiene su ausencia registrada; la prueba P19 demuestra que
  detecta 29 días cuando se agrega una semana sin checadas.

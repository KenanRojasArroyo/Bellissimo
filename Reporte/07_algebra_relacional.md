# 8. Álgebra relacional

Cada expresión se comprobó con su SQL equivalente (`Sql/algebra_relacional.sql`) en **MySQL 8.0.46**
el 2026-11-16, sobre la base recién reconstruida con `Sql/01–04` (datos ficticios). Los
resultados de esta página son los que **devolvió MySQL**, no cálculos a mano.

Notación: σ selección · π proyección · ⋈ reunión (join) · ∪ unión · − diferencia ·
∩ intersección · γ agrupación con funciones de agregado.

Relaciones usadas (atributos principales):
`empleado(id_empleado, id_puesto, nombre, apellido_paterno, …, sueldo_base_semanal)`,
`puesto(id_puesto, nombre)`, `ausencia(id_ausencia, id_empleado, fecha, tipo)`,
`pago_semanal(id_pago, id_empleado, id_semana, …, salario_final)`,
`detalle_pago(id_detalle, id_pago, fecha, concepto, minutos, monto)`.

---

### AR1 — Selección (σ)

**Pregunta:** ¿qué empleados ganan más de $3,000 a la semana?

```text
σ sueldo_base_semanal > 3000 (empleado)
```

```sql
SELECT DISTINCT * FROM empleado WHERE sueldo_base_semanal > 3000;
```

**Resultado observado (2 tuplas):**

| id_empleado | nombre | apellido_paterno | sueldo_base_semanal |
| ----------- | ------ | ---------------- | ------------------- |
| 2 | Juan | Pérez | 3300.00 |
| 4 | Luis | Ramírez | 3150.00 |

(MySQL devolvió todas las columnas de `empleado`; aquí se muestran las relevantes.)

### AR2 — Proyección sobre una selección (π, σ)

**Pregunta:** ¿cómo se llaman los floristas (puesto 3)?

```text
π nombre, apellido_paterno ( σ id_puesto = 3 (empleado) )
```

```sql
SELECT DISTINCT nombre, apellido_paterno FROM empleado WHERE id_puesto = 3;
```

**Resultado observado (2 tuplas):** (Ana, Torres), (Luis, Ramírez).

### AR3 — Reunión (⋈)

**Pregunta:** ¿qué puesto tiene cada empleado?

```text
π empleado.nombre, empleado.apellido_paterno, puesto.nombre
   ( empleado ⋈ empleado.id_puesto = puesto.id_puesto  puesto )
```

```sql
SELECT DISTINCT e.nombre, e.apellido_paterno, p.nombre AS puesto
FROM empleado e INNER JOIN puesto p ON e.id_puesto = p.id_puesto;
```

**Resultado observado (5 tuplas):**

| nombre | apellido_paterno | puesto |
| ------ | ---------------- | ------ |
| Ana | Torres | Florista |
| Luis | Ramírez | Florista |
| María | López | Recepcionista |
| Carla | Mendoza | Recepcionista |
| Juan | Pérez | Repartidor |

### AR4 — Selección, reunión y proyección

**Pregunta:** ¿quién tuvo faltas injustificadas y en qué fecha?

```text
π nombre, apellido_paterno, fecha
   ( σ tipo = 'Injustificada' (ausencia) ⋈ ausencia.id_empleado = empleado.id_empleado empleado )
```

```sql
SELECT DISTINCT e.nombre, e.apellido_paterno, a.fecha
FROM (SELECT * FROM ausencia WHERE tipo = 'Injustificada') a
INNER JOIN empleado e ON a.id_empleado = e.id_empleado;
```

**Resultado observado (2 tuplas):** (Juan, Pérez, 2026-10-29), (Ana, Torres, 2026-11-04).

### AR5 — Diferencia (−)

**Pregunta:** ¿qué empleados todavía no tienen ningún pago generado?

```text
π id_empleado (empleado)  −  π id_empleado (pago_semanal)
```

```sql
SELECT id_empleado FROM empleado
EXCEPT
SELECT id_empleado FROM pago_semanal;       -- EXCEPT existe desde MySQL 8.0.31
```

**Resultado observado (1 tupla):** {5} → Carla Mendoza, que ingresó el 2026-11-09, después de
las dos semanas de prueba.

### AR6 — Unión (∪)

**Pregunta:** ¿qué empleados tuvieron alguna incidencia (ausencia registrada o retardo)?

```text
π id_empleado (ausencia)
   ∪
π id_empleado ( σ concepto = 'RETARDO' (detalle_pago) ⋈ detalle_pago.id_pago = pago_semanal.id_pago pago_semanal )
```

```sql
SELECT id_empleado FROM ausencia
UNION
SELECT p.id_empleado
FROM detalle_pago d INNER JOIN pago_semanal p ON d.id_pago = p.id_pago
WHERE d.concepto = 'RETARDO';
```

**Resultado observado (4 tuplas):** {1, 2, 3, 4}. Los dos operandos también se consultaron por
separado: con ausencia, {2, 3, 4}; con retardo, {1, 2} (María el 2026-11-04 con 10 min; Juan el
27, 28 y 30 de octubre con 12, 7 y 20 min). La unión elimina el 2 repetido.

### AR7 — Agrupación con agregados (γ)

**Pregunta:** ¿cuánto se paga en total cada semana y a cuántas personas?

```text
id_semana γ SUM(salario_final) → total, COUNT(id_pago) → pagos (pago_semanal)
```

```sql
SELECT id_semana, SUM(salario_final) AS total, COUNT(id_pago) AS pagos
FROM pago_semanal GROUP BY id_semana;
```

**Resultado observado (2 tuplas):**

| id_semana | total | pagos |
| --------- | ----- | ----- |
| 1 | 11868.73 | 4 |
| 2 | 14562.50 | 4 |

Coincide con los totales de regresión del proyecto.

### AR8 — Intersección (∩)

**Pregunta:** ¿quiénes tienen pago en la semana 1 **y** en la semana 2?

```text
π id_empleado ( σ id_semana = 1 (pago_semanal) )  ∩  π id_empleado ( σ id_semana = 2 (pago_semanal) )
```

```sql
SELECT id_empleado FROM pago_semanal WHERE id_semana = 1
INTERSECT
SELECT id_empleado FROM pago_semanal WHERE id_semana = 2;   -- INTERSECT desde MySQL 8.0.31
```

**Resultado observado (4 tuplas):** {1, 2, 3, 4}.

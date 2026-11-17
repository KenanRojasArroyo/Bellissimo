/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : algebra_relacional.sql
   Motor   : MySQL 8.0

   SQL equivalente a cada expresión de álgebra relacional del reporte
   (Reporte/algebra_relacional.md). Ejecutar sobre la base recién
   reconstruida con 01 -> 04 (antes de Pruebas/pruebas_bd.sql).
   Se usa DISTINCT porque el álgebra relacional trabaja con conjuntos.
   ===================================================================== */

USE belissimo;

-- AR1  σ  sueldo_base_semanal > 3000 (empleado)
SELECT DISTINCT *
FROM empleado
WHERE sueldo_base_semanal > 3000;

-- AR2  π nombre, apellido_paterno ( σ id_puesto = 3 (empleado) )
SELECT DISTINCT nombre, apellido_paterno
FROM empleado
WHERE id_puesto = 3;

-- AR3  π empleado.nombre, empleado.apellido_paterno, puesto.nombre
--        ( empleado ⋈ empleado.id_puesto = puesto.id_puesto  puesto )
SELECT DISTINCT e.nombre, e.apellido_paterno, p.nombre AS puesto
FROM empleado e
INNER JOIN puesto p ON e.id_puesto = p.id_puesto;

-- AR4  π nombre, apellido_paterno, fecha
--        ( σ tipo = 'Injustificada' (ausencia) ⋈ empleado )
SELECT DISTINCT e.nombre, e.apellido_paterno, a.fecha
FROM (SELECT * FROM ausencia WHERE tipo = 'Injustificada') a
INNER JOIN empleado e ON a.id_empleado = e.id_empleado;

-- AR5  Diferencia: π id_empleado (empleado) − π id_empleado (pago_semanal)
--      (empleados sin ningún pago generado)
--      (EXCEPT existe desde MySQL 8.0.31)
SELECT id_empleado FROM empleado
EXCEPT
SELECT id_empleado FROM pago_semanal;

-- AR6  Unión: π id_empleado (ausencia)
--        ∪ π id_empleado ( σ concepto = 'RETARDO' (detalle_pago) ⋈ pago_semanal )
--      (empleados con alguna ausencia o algún retardo)
SELECT id_empleado FROM ausencia
UNION
SELECT p.id_empleado
FROM detalle_pago d
INNER JOIN pago_semanal p ON d.id_pago = p.id_pago
WHERE d.concepto = 'RETARDO'
ORDER BY id_empleado;

-- AR7  Agrupación: id_semana γ SUM(salario_final) → total, COUNT(id_pago) → pagos (pago_semanal)
SELECT id_semana, SUM(salario_final) AS total, COUNT(id_pago) AS pagos
FROM pago_semanal
GROUP BY id_semana;

-- AR8  Intersección: π id_empleado ( σ id_semana = 1 (pago_semanal) )
--                  ∩ π id_empleado ( σ id_semana = 2 (pago_semanal) )
--      (INTERSECT existe desde MySQL 8.0.31)
SELECT id_empleado FROM pago_semanal WHERE id_semana = 1
INTERSECT
SELECT id_empleado FROM pago_semanal WHERE id_semana = 2
ORDER BY id_empleado;

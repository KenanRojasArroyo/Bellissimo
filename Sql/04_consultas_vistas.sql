/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : 04_consultas_vistas.sql
   Motor   : MySQL 8.0 (MySQL Workbench)

   Contiene: preparación de pagos, 2 vistas y 10 consultas documentadas
             (3 con JOIN, 2 con subconsultas, 3 con GROUP BY/HAVING y 2 libres).

   Ejecutar después de 03_rutinas.sql y ANTES de 05_pruebas.sql.
   Si ya ejecutaste 05_pruebas.sql, reconstruye la base desde 01_esquema.sql.
   Ejecuta cada consulta por separado (cursor sobre ella + rayo) para ver su resultado.
   ===================================================================== */

USE belissimo;

/* ---------------------------------------------------------------------
   0. PREPARACIÓN: generar los pagos de las dos semanas
      (usuario 2 = encargado de prueba). Los 5 empleados con checadas
      se calculan con el procedimiento; Carla no tiene checadas.
   --------------------------------------------------------------------- */

CALL sp_generar_pago(1, 1, 2);
CALL sp_generar_pago(2, 1, 2);
CALL sp_generar_pago(3, 1, 2);
CALL sp_generar_pago(4, 1, 2);
CALL sp_generar_pago(1, 2, 2);
CALL sp_generar_pago(2, 2, 2);
CALL sp_generar_pago(3, 2, 2);
CALL sp_generar_pago(4, 2, 2);

/* ---------------------------------------------------------------------
   1. VISTAS
   --------------------------------------------------------------------- */

/* VISTA 1: v_asistencia_diaria
   Propósito: una fila por empleado y día con las horas presentes y los minutos
   de retardo, comida de más, salida temprana y tiempo extra. Es la base del
   reporte de asistencia de la página web. */
CREATE OR REPLACE VIEW v_asistencia_diaria AS
SELECT
    r.id_registro,
    e.id_empleado,
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    r.fecha,
    TIME(r.entrada) AS entrada,
    TIME(r.salida)  AS salida,
    ROUND(IFNULL(TIMESTAMPDIFF(MINUTE, r.entrada, r.salida), 0) / 60, 2) AS horas_presentes,
    IFNULL(TIMESTAMPDIFF(MINUTE, r.salida_comida, r.regreso_comida), 0) AS min_comida,
    fn_minutos_comida_extra(r.salida_comida, r.regreso_comida) AS min_comida_extra,
    CASE
        WHEN r.entrada > TIMESTAMP(r.fecha, ADDTIME(h.hora_entrada, SEC_TO_TIME(c.tolerancia * 60)))
        THEN TIMESTAMPDIFF(MINUTE, TIMESTAMP(r.fecha, h.hora_entrada), r.entrada)
        ELSE 0
    END AS min_retardo,
    IFNULL(GREATEST(TIMESTAMPDIFF(MINUTE, r.salida, TIMESTAMP(r.fecha, h.hora_salida)), 0), 0) AS min_salida_temprana,
    IFNULL(GREATEST(TIMESTAMPDIFF(MINUTE, TIMESTAMP(r.fecha, h.hora_salida), r.salida), 0), 0) AS min_extra,
    r.metodo
FROM registro_asistencia r
INNER JOIN empleado e ON e.id_empleado = r.id_empleado
INNER JOIN horario_general h ON h.dia_semana = WEEKDAY(r.fecha) + 1
CROSS JOIN (SELECT CAST(valor AS UNSIGNED) AS tolerancia
            FROM configuracion WHERE parametro = 'tolerancia_min') c;

/* VISTA 2: v_resumen_pago_semanal
   Propósito: el pago de cada empleado por semana con su puesto y su estado.
   Es la pantalla principal del encargado antes de abrir el ticket. */
CREATE OR REPLACE VIEW v_resumen_pago_semanal AS
SELECT
    p.id_pago,
    s.id_semana,
    s.fecha_inicio,
    s.fecha_fin,
    e.id_empleado,
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    pu.nombre AS puesto,
    p.sueldo_base_usado AS sueldo_base,
    p.descuento_total,
    p.monto_extra,
    p.salario_final,
    IF(p.cerrado = 1, 'Cerrado', 'Borrador') AS estado
FROM pago_semanal p
INNER JOIN empleado e ON e.id_empleado = p.id_empleado
INNER JOIN puesto pu ON pu.id_puesto = e.id_puesto
INNER JOIN semana s ON s.id_semana = p.id_semana;

-- Demostración de las vistas
SELECT * FROM v_asistencia_diaria WHERE id_empleado = 2 ORDER BY fecha;
SELECT * FROM v_resumen_pago_semanal ORDER BY id_semana, id_empleado;

/* ---------------------------------------------------------------------
   2. CONSULTAS CON JOIN (3)
   --------------------------------------------------------------------- */

/* CONSULTA 1 (INNER JOIN)
   Pregunta: ¿qué empleados hay, en qué puesto y con qué sueldo base semanal? */
SELECT
    e.id_empleado,
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    pu.nombre AS puesto,
    e.sueldo_base_semanal,
    e.fecha_ingreso
FROM empleado e
INNER JOIN puesto pu
    ON pu.id_puesto = e.id_puesto
ORDER BY pu.nombre, e.apellido_paterno;

/* CONSULTA 2 (JOIN de 4 tablas)
   Pregunta: ¿qué descuentos tuvo cada empleado en la semana 1 y de qué día? */
SELECT
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    d.fecha,
    d.concepto,
    d.minutos,
    d.monto
FROM detalle_pago d
INNER JOIN pago_semanal p ON p.id_pago = d.id_pago
INNER JOIN empleado e ON e.id_empleado = p.id_empleado
INNER JOIN semana s ON s.id_semana = p.id_semana
WHERE s.fecha_inicio = '2026-10-26'
  AND d.monto < 0
ORDER BY e.id_empleado, d.fecha;

/* CONSULTA 3 (LEFT JOIN)
   Pregunta: ¿cuántos pagos tiene cada empleado? Debe aparecer también quien
   todavía no tiene ninguno (Carla, empleada nueva). */
SELECT
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    COUNT(p.id_pago) AS pagos_generados,
    MAX(p.fecha_generacion) AS ultimo_pago
FROM empleado e
LEFT JOIN pago_semanal p
    ON p.id_empleado = e.id_empleado
GROUP BY e.id_empleado, e.nombre, e.apellido_paterno
ORDER BY e.id_empleado;

/* ---------------------------------------------------------------------
   3. SUBCONSULTAS (2)
   --------------------------------------------------------------------- */

/* CONSULTA 4 (subconsulta escalar)
   Pregunta: ¿qué empleados cobraron más que el promedio en la semana 2? */
SELECT
    empleado,
    puesto,
    salario_final
FROM v_resumen_pago_semanal
WHERE id_semana = 2
  AND salario_final > (SELECT AVG(salario_final)
                       FROM pago_semanal
                       WHERE id_semana = 2)
ORDER BY salario_final DESC;

/* CONSULTA 5 (NOT EXISTS)
   Pregunta: ¿qué empleados activos no tienen ninguna checada registrada?
   (sirve para detectar a quien nunca ha checado o es de nuevo ingreso) */
SELECT
    e.id_empleado,
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    e.fecha_ingreso
FROM empleado e
WHERE e.activo = 1
  AND NOT EXISTS (SELECT 1
                  FROM registro_asistencia r
                  WHERE r.id_empleado = e.id_empleado);

/* ---------------------------------------------------------------------
   4. FUNCIONES AGREGADAS CON GROUP BY / HAVING (3)
   --------------------------------------------------------------------- */

/* CONSULTA 6 (SUM + GROUP BY + HAVING)
   Pregunta: ¿qué empleados acumulan retardos o comida de más? */
SELECT
    empleado,
    SUM(min_retardo)       AS total_min_retardo,
    SUM(min_comida_extra)  AS total_min_comida_extra
FROM v_asistencia_diaria
GROUP BY id_empleado, empleado
HAVING SUM(min_retardo) + SUM(min_comida_extra) > 0
ORDER BY total_min_comida_extra DESC, total_min_retardo DESC;

/* CONSULTA 7 (COUNT, SUM, AVG, MAX + GROUP BY)
   Pregunta: en cada semana, ¿cuánto se paga en total, cuánto se descuenta y
   cuánto se agrega por tiempo extra? */
SELECT
    s.fecha_inicio,
    s.fecha_fin,
    COUNT(*)                  AS pagos,
    SUM(p.salario_final)      AS total_a_pagar,
    ROUND(AVG(p.salario_final), 2) AS promedio,
    MAX(p.salario_final)      AS maximo,
    SUM(p.descuento_total)    AS total_descuentos,
    SUM(p.monto_extra)        AS total_extra
FROM pago_semanal p
INNER JOIN semana s ON s.id_semana = p.id_semana
GROUP BY s.id_semana, s.fecha_inicio, s.fecha_fin
ORDER BY s.fecha_inicio;

/* CONSULTA 8 (COUNT + GROUP BY + HAVING)
   Pregunta: ¿qué puestos tienen incidencias que descuentan dinero, y cuántas? */
SELECT
    pu.nombre AS puesto,
    COUNT(*) AS incidencias_con_descuento,
    SUM(d.minutos) AS minutos_descontados,
    SUM(d.monto) AS monto_descontado
FROM detalle_pago d
INNER JOIN pago_semanal p ON p.id_pago = d.id_pago
INNER JOIN empleado e ON e.id_empleado = p.id_empleado
INNER JOIN puesto pu ON pu.id_puesto = e.id_puesto
WHERE d.monto < 0
GROUP BY pu.id_puesto, pu.nombre
HAVING COUNT(*) >= 1
ORDER BY monto_descontado;

/* ---------------------------------------------------------------------
   5. CONSULTAS DE LIBRE ELECCIÓN (2)
   --------------------------------------------------------------------- */

/* CONSULTA 9 (función de ventana RANK)
   Pregunta: ¿quién fue más puntual cada semana? Se ordena por los minutos
   perdidos (retardo + salida temprana + comida de más); menos es mejor. */
SELECT
    s.fecha_inicio AS semana,
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    p.min_retardo + p.min_salida_temprana + p.min_exceso_comida AS minutos_perdidos,
    RANK() OVER (PARTITION BY p.id_semana
                 ORDER BY p.min_retardo + p.min_salida_temprana + p.min_exceso_comida) AS lugar
FROM pago_semanal p
INNER JOIN empleado e ON e.id_empleado = p.id_empleado
INNER JOIN semana s ON s.id_semana = p.id_semana
ORDER BY s.fecha_inicio, lugar;

/* CONSULTA 10 (tabla cruzada con SUM(CASE))
   Pregunta: ¿cómo queda el ticket de cada empleado, concepto por concepto,
   en una sola fila por semana? */
SELECT
    s.fecha_inicio AS semana,
    CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
    SUM(CASE WHEN d.concepto = 'COMIDA_EXCESO'       THEN d.monto ELSE 0 END) AS comida,
    SUM(CASE WHEN d.concepto = 'SALIDA_TEMPRANA'     THEN d.monto ELSE 0 END) AS salida_temprana,
    SUM(CASE WHEN d.concepto = 'FALTA_INJUSTIFICADA' THEN d.monto ELSE 0 END) AS faltas,
    SUM(CASE WHEN d.concepto = 'TIEMPO_EXTRA'        THEN d.monto ELSE 0 END) AS tiempo_extra,
    SUM(d.monto) AS neto
FROM detalle_pago d
INNER JOIN pago_semanal p ON p.id_pago = d.id_pago
INNER JOIN empleado e ON e.id_empleado = p.id_empleado
INNER JOIN semana s ON s.id_semana = p.id_semana
GROUP BY s.id_semana, s.fecha_inicio, e.id_empleado, e.nombre, e.apellido_paterno
ORDER BY s.fecha_inicio, e.id_empleado;

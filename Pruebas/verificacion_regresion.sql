/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : verificacion_regresion.sql
   Motor   : MySQL 8.0 (MySQL Workbench)

   Prueba de regresión: confirma que después de reconstruir la base con
   01 -> 02 -> 03 -> 04 los objetos y los pagos son los esperados.
   Ejecutar JUSTO después de 04_consultas_vistas.sql (antes de pruebas_bd.sql,
   porque las pruebas cambian datos a propósito).

   Cada fila muestra lo esperado, lo obtenido y OK / FALLA.
   ===================================================================== */

USE belissimo;

SELECT 'Objetos' AS grupo, t.concepto, t.esperado, t.obtenido,
       IF(t.esperado = t.obtenido, 'OK', 'FALLA') AS estado
FROM (
    SELECT 'tablas' AS concepto, '11' AS esperado,
           CAST((SELECT COUNT(*) FROM information_schema.TABLES
                 WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE') AS CHAR) AS obtenido
    UNION ALL
    SELECT 'vistas', '3',
           CAST((SELECT COUNT(*) FROM information_schema.VIEWS
                 WHERE TABLE_SCHEMA = DATABASE()) AS CHAR)
    UNION ALL
    SELECT 'funciones', '3',
           CAST((SELECT COUNT(*) FROM information_schema.ROUTINES
                 WHERE ROUTINE_SCHEMA = DATABASE() AND ROUTINE_TYPE = 'FUNCTION') AS CHAR)
    UNION ALL
    SELECT 'procedimientos', '5',
           CAST((SELECT COUNT(*) FROM information_schema.ROUTINES
                 WHERE ROUTINE_SCHEMA = DATABASE() AND ROUTINE_TYPE = 'PROCEDURE') AS CHAR)
    UNION ALL
    SELECT 'triggers', '12',
           CAST((SELECT COUNT(*) FROM information_schema.TRIGGERS
                 WHERE TRIGGER_SCHEMA = DATABASE()) AS CHAR)
    UNION ALL
    SELECT 'checadas (registro_asistencia)', '44',
           CAST((SELECT COUNT(*) FROM registro_asistencia) AS CHAR)
    UNION ALL
    SELECT 'ausencias', '4', CAST((SELECT COUNT(*) FROM ausencia) AS CHAR)
    UNION ALL
    SELECT 'empleados', '5', CAST((SELECT COUNT(*) FROM empleado) AS CHAR)
    UNION ALL
    SELECT 'pagos', '8', CAST((SELECT COUNT(*) FROM pago_semanal) AS CHAR)
    UNION ALL
    SELECT 'pagos de Carla (id 5)', '0',
           CAST((SELECT COUNT(*) FROM pago_semanal WHERE id_empleado = 5) AS CHAR)
    UNION ALL
    SELECT 'dias sin checada ni ausencia', '0',
           CAST((SELECT COUNT(*) FROM v_dias_sin_checada) AS CHAR)
) t

UNION ALL

SELECT 'Pago', CONCAT('semana ', e.id_semana, ' - ', e.empleado), e.esperado,
       CAST(p.salario_final AS CHAR),
       IF(p.salario_final = e.esperado, 'OK', 'FALLA')
FROM (
    SELECT 1 AS id_semana, 1 AS id_empleado, 'María López'  AS empleado, 2911.45 AS esperado
    UNION ALL SELECT 1, 2, 'Juan Pérez',   2807.28
    UNION ALL SELECT 1, 3, 'Ana Torres',   3000.00
    UNION ALL SELECT 1, 4, 'Luis Ramírez', 3150.00
    UNION ALL SELECT 2, 1, 'María López',  3000.00
    UNION ALL SELECT 2, 2, 'Juan Pérez',   5156.25
    UNION ALL SELECT 2, 3, 'Ana Torres',   2468.75
    UNION ALL SELECT 2, 4, 'Luis Ramírez', 3937.50
) e
LEFT JOIN pago_semanal p
       ON p.id_empleado = e.id_empleado AND p.id_semana = e.id_semana

UNION ALL

SELECT 'Detalle', 'Juan semana 1: descuento / extra', '630.22 / 137.50',
       CONCAT(descuento_total, ' / ', monto_extra),
       IF(descuento_total = 630.22 AND monto_extra = 137.50, 'OK', 'FALLA')
FROM pago_semanal WHERE id_empleado = 2 AND id_semana = 1

UNION ALL

SELECT 'Total', CONCAT('semana ', id_semana), IF(id_semana = 1, '11868.73', '14562.50'),
       CAST(SUM(salario_final) AS CHAR),
       IF(SUM(salario_final) = IF(id_semana = 1, 11868.73, 14562.50), 'OK', 'FALLA')
FROM pago_semanal
GROUP BY id_semana;

/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : 05_pruebas.sql
   Motor   : MySQL 8.0 (MySQL Workbench)

   Casos de prueba con operaciones válidas e inválidas, triggers,
   procedimientos, funciones, transacciones y persistencia.

   Ejecutar después de 04_consultas_vistas.sql, EN ORDEN (algunas pruebas
   usan datos creados por las anteriores).
   Cada prueba indica: requisito, precondición, entrada y resultado ESPERADO.
   El resultado OBSERVADO y el estado se llenan en la matriz de pruebas
   al ejecutarlas.

   Las pruebas marcadas "POR SEPARADO" producen un error A PROPÓSITO:
   ejecútalas una por una, descomentando solo la sentencia que vas a probar.
   ===================================================================== */

USE belissimo;

/* ---------------------------------------------------------------------
   P01  ALTA válida de un empleado
   Requisito : CRUD de empleado (INSERT con validación de dominio)
   Precondición: existe el puesto 3 (Florista)
   Entrada   : empleado nuevo con sueldo base 3200
   Esperado  : se inserta 1 fila y el SELECT la muestra
   --------------------------------------------------------------------- */
INSERT INTO empleado
(id_puesto, nombre, apellido_paterno, telefono, fecha_ingreso, sueldo_base_semanal)
VALUES
(3, 'Prueba', 'Temporal', '6460000099', '2026-11-09', 3200.00);

SELECT id_empleado, nombre, apellido_paterno, sueldo_base_semanal
FROM empleado
WHERE apellido_paterno = 'Temporal';

/* ---------------------------------------------------------------------
   P02  CONSULTA con filtro y ordenamiento
   Requisito : búsqueda de empleados (módulo de consulta)
   Precondición: P01 ejecutada
   Entrada   : puesto Florista, ordenado por sueldo de mayor a menor
   Esperado  : 3 filas: Prueba (3200), Luis (3150) y Ana (3000)
   --------------------------------------------------------------------- */
SELECT CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado, e.sueldo_base_semanal
FROM empleado e
INNER JOIN puesto p ON p.id_puesto = e.id_puesto
WHERE p.nombre = 'Florista'
ORDER BY e.sueldo_base_semanal DESC;

/* ---------------------------------------------------------------------
   P03  MODIFICACIÓN válida
   Requisito : CRUD de empleado (UPDATE)
   Precondición: P01 ejecutada
   Entrada   : cambiar el teléfono del empleado de prueba
   Esperado  : 1 fila afectada; el SELECT muestra el nuevo teléfono
   --------------------------------------------------------------------- */
UPDATE empleado
SET telefono = '6461112233'
WHERE apellido_paterno = 'Temporal';

SELECT nombre, telefono FROM empleado WHERE apellido_paterno = 'Temporal';

/* ---------------------------------------------------------------------
   P04  VALIDACIÓN NEGATIVA de dominio            -- POR SEPARADO
   Requisito : CHECK sueldo_base_semanal > 0
   Entrada   : sueldo base negativo
   Esperado  : ERROR 3819 (MySQL) / 4025 (MariaDB): falla ck_empleado_sueldo;
               no se modifica nada
   --------------------------------------------------------------------- */
-- UPDATE empleado SET sueldo_base_semanal = -100
-- WHERE apellido_paterno = 'Temporal';

/* ---------------------------------------------------------------------
   P05  ELIMINACIÓN bloqueada por integridad referencial   -- POR SEPARADO
   Requisito : llaves foráneas con ON DELETE RESTRICT
   Precondición: María (id 1) tiene checadas y pagos
   Esperado  : ERROR 1451: no se puede borrar un registro padre con hijos
   --------------------------------------------------------------------- */
-- DELETE FROM empleado WHERE id_empleado = 1;

/* ---------------------------------------------------------------------
   P06  ELIMINACIÓN válida
   Requisito : CRUD de empleado (DELETE)
   Precondición: el empleado de prueba no tiene checadas ni pagos
   Esperado  : 1 fila eliminada; el SELECT ya no devuelve filas
   --------------------------------------------------------------------- */
DELETE FROM empleado WHERE apellido_paterno = 'Temporal';

SELECT COUNT(*) AS filas_restantes FROM empleado WHERE apellido_paterno = 'Temporal';

/* ---------------------------------------------------------------------
   P07  PROCEDIMIENTO sp_registrar_checada: las 4 checadas del día
   Requisito : registrar fecha y hora exacta de entrada y salida
   Precondición: Carla (id 5) no tiene checadas; el lunes 9 nov 2026 es día hábil
   Entrada   : cuatro llamadas en orden con la hora de cada checada
   Esperado  : responde ENTRADA, SALIDA_COMIDA, REGRESO_COMIDA y SALIDA;
               queda 1 fila con las cuatro horas y método Clave
   --------------------------------------------------------------------- */
CALL sp_registrar_checada(5, '2026-11-09 09:02:00', 'Clave');
CALL sp_registrar_checada(5, '2026-11-09 13:00:00', 'Clave');
CALL sp_registrar_checada(5, '2026-11-09 13:35:00', 'Clave');
CALL sp_registrar_checada(5, '2026-11-09 17:30:00', 'Clave');

SELECT * FROM registro_asistencia WHERE id_empleado = 5;

/* ---------------------------------------------------------------------
   P08  VALIDACIÓN NEGATIVA del procedimiento     -- POR SEPARADO
   Requisito : no se permiten más de 4 checadas al día
   Precondición: P07 ejecutada
   Esperado  : ERROR 1644: "El empleado ya tiene las cuatro checadas de hoy."
   --------------------------------------------------------------------- */
-- CALL sp_registrar_checada(5, '2026-11-09 18:00:00', 'Clave');

/* ---------------------------------------------------------------------
   P09  FUNCIONES almacenadas
   Requisito : cálculos de horas y valor de la hora
   Esperado  : fn_valor_hora(3300) = 68.7500 (3300 / 48)
               fn_minutos_comida_extra(13:00 a 13:35) = 5
               fn_semana_cerrada(1, '2026-10-26') = 0 (todavía no se cierra)
   --------------------------------------------------------------------- */
SELECT
    fn_valor_hora(3300.00) AS valor_hora,
    fn_minutos_comida_extra('2026-11-09 13:00:00', '2026-11-09 13:35:00') AS comida_extra,
    fn_semana_cerrada(1, '2026-10-26') AS semana_cerrada;

/* ---------------------------------------------------------------------
   P10  PROCEDIMIENTO sp_generar_pago y ticket
   Requisito : total a pagar con descuentos y tiempo extra
   Precondición: pagos generados en 04_consultas_vistas.sql
   Entrada   : volver a generar el pago de Juan, semana 1 (permitido: está en borrador)
   Esperado  : descuento 630.22, tiempo extra 137.50, salario 2807.28
               (falta 584.38 + salida 34.38 + comida 11.46; 60 min extra x2)
   --------------------------------------------------------------------- */
CALL sp_generar_pago(2, 1, 2);
CALL sp_ticket(2, 1);

/* ---------------------------------------------------------------------
   P11  TRANSACCIÓN con COMMIT
   Requisito : operaciones transaccionales
   Entrada   : cambiar el teléfono de Ana y confirmar
   Esperado  : el cambio queda guardado
   --------------------------------------------------------------------- */
START TRANSACTION;
UPDATE empleado SET telefono = '6469998877' WHERE id_empleado = 3;
COMMIT;

SELECT id_empleado, telefono FROM empleado WHERE id_empleado = 3;

/* ---------------------------------------------------------------------
   P12  TRANSACCIÓN con ROLLBACK (caso controlado)
   Requisito : operaciones transaccionales
   Entrada   : subir el sueldo de Ana y de Luis, revisar y deshacer
   Esperado  : dentro de la transacción se ven 3500 y 3650; después del
               ROLLBACK vuelven a 3000 y 3150
   --------------------------------------------------------------------- */
START TRANSACTION;
UPDATE empleado SET sueldo_base_semanal = 3500 WHERE id_empleado = 3;
UPDATE empleado SET sueldo_base_semanal = 3650 WHERE id_empleado = 4;
SELECT id_empleado, sueldo_base_semanal FROM empleado WHERE id_empleado IN (3, 4);
ROLLBACK;

SELECT id_empleado, sueldo_base_semanal FROM empleado WHERE id_empleado IN (3, 4);

/* ---------------------------------------------------------------------
   P13  TRIGGER de auditoría (trg_registro_au_auditoria)
   Requisito : registrar quién corrigió una checada y qué cambió
   Precondición: María llegó a las 09:10 el miércoles 4 nov (retardo de 10 min)
   Entrada   : el encargado (usuario 2) corrige la entrada a las 08:58
   Esperado  : se crea 1 fila en auditoria_registro con la hora anterior (09:10)
               y la nueva (08:58); al regenerar su pago, el retardo desaparece
               (min_retardo = 0). La aplicación NO escribe la auditoría: solo el trigger.
   --------------------------------------------------------------------- */
UPDATE registro_asistencia
SET entrada = '2026-11-04 08:58:00', metodo = 'Manual', modificado_por = 2
WHERE id_empleado = 1 AND fecha = '2026-11-04';

SELECT id_registro, fecha, entrada_anterior, entrada_nueva, id_usuario_app, usuario_bd
FROM auditoria_registro;

CALL sp_generar_pago(1, 2, 2);
SELECT min_retardo, salario_final FROM pago_semanal WHERE id_empleado = 1 AND id_semana = 2;

/* ---------------------------------------------------------------------
   P14  TRIGGERS de semana cerrada
   Requisito : un pago cerrado ya no se puede alterar
   Entrada   : cerrar el pago de María, semana 1 (id_pago 1) y agregar una observación
   Esperado  : estado Cerrado; la observación sí se guarda
   --------------------------------------------------------------------- */
CALL sp_cerrar_pago(1);
CALL sp_agregar_observacion(1, 'Pago revisado con la empleada: 85 min de comida de mas.');

SELECT id_pago, cerrado, salario_final, observaciones FROM pago_semanal WHERE id_pago = 1;

/* P14a  Regenerar un pago cerrado               -- POR SEPARADO
   Esperado: ERROR 1644 "El pago de esta semana ya esta cerrado..." */
-- CALL sp_generar_pago(1, 1, 2);

/* P14b  Corregir una checada de una semana cerrada   -- POR SEPARADO
   (trg_registro_bu_cerrado)
   Esperado: ERROR 1644 "La semana de esa checada ya esta cerrada..." */
-- UPDATE registro_asistencia SET salida = '2026-10-26 17:00:00'
-- WHERE id_empleado = 1 AND fecha = '2026-10-26';

/* P14c  Insertar una checada en una semana cerrada   -- POR SEPARADO
   (trg_registro_bi_cerrado). Se usa un lunes que ya tiene registro de otro
   empleado para no chocar con otras reglas.
   Esperado: ERROR 1644 "La semana de esa checada ya esta cerrada..." */
-- INSERT INTO registro_asistencia (id_empleado, fecha, entrada)
-- VALUES (1, '2026-10-26', '2026-10-26 09:00:00');

/* P14d  Alterar un renglón del ticket cerrado   -- POR SEPARADO
   (trg_detalle_bu_cerrado)
   Esperado: ERROR 1644 "El pago esta cerrado: su ticket no se puede modificar." */
-- UPDATE detalle_pago SET monto = 0 WHERE id_pago = 1;

/* P14e  Cambiar el salario de un pago cerrado   -- POR SEPARADO
   (trg_pago_bu_cerrado)
   Esperado: ERROR 1644 "El pago esta cerrado: solo se pueden agregar observaciones." */
-- UPDATE pago_semanal SET salario_final = 9999 WHERE id_pago = 1;

/* ---------------------------------------------------------------------
   P15  Otras validaciones de integridad          -- POR SEPARADO
   Cada sentencia debe producir error:
   a) checada de un empleado que no existe  -> ERROR 1452 (llave foránea)
   b) dos registros del mismo empleado y día -> ERROR 1062 (UNIQUE)
   c) regreso de comida antes de la salida   -> falla ck_registro_orden
   d) fecha distinta a la de la entrada      -> falla ck_registro_fecha
   --------------------------------------------------------------------- */
-- a)
-- INSERT INTO registro_asistencia (id_empleado, fecha, entrada)
-- VALUES (999, '2026-11-10', '2026-11-10 09:00:00');
-- b)
-- INSERT INTO registro_asistencia (id_empleado, fecha, entrada)
-- VALUES (5, '2026-11-09', '2026-11-09 09:00:00');
-- c)
-- INSERT INTO registro_asistencia (id_empleado, fecha, entrada, salida_comida, regreso_comida)
-- VALUES (3, '2026-11-10', '2026-11-10 09:00:00', '2026-11-10 13:00:00', '2026-11-10 12:00:00');
-- d)
-- INSERT INTO registro_asistencia (id_empleado, fecha, entrada)
-- VALUES (3, '2026-11-10', '2026-11-11 09:00:00');

/* ---------------------------------------------------------------------
   P16  REPORTE con vista y persistencia
   Requisito : módulo de reporte y persistencia de datos
   Entrada   : consultar el resumen de pagos; luego CERRAR la conexión de
               Workbench, abrir una nueva y volver a ejecutar el SELECT
   Esperado  : los mismos datos (el pago 1 en estado Cerrado, la auditoría
               de P13, las 4 checadas de Carla): lo guardado persiste
   --------------------------------------------------------------------- */
SELECT * FROM v_resumen_pago_semanal ORDER BY id_semana, id_empleado;
SELECT COUNT(*) AS checadas_de_carla FROM registro_asistencia WHERE id_empleado = 5;
SELECT COUNT(*) AS correcciones_auditadas FROM auditoria_registro;

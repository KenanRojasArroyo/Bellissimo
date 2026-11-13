/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : 03_rutinas.sql
   Motor   : MySQL 8.0 (MySQL Workbench)

   Funciones, procedimientos almacenados y triggers.
   Ejecutar después de 01_esquema.sql y 02_datos.sql.

   NOTA: con el registro binario activo (MySQL 8 lo trae activado), crear
   triggers y funciones exige permiso SUPER. El usuario root de Workbench
   lo tiene. Si usas otro usuario y marca el error 1419, ejecuta como root:
       SET GLOBAL log_bin_trust_function_creators = 1;
   ===================================================================== */

USE belissimo;

DELIMITER $$

/* =====================================================================
   FUNCIONES
   ===================================================================== */

/* fn_valor_hora
   Valor de una hora de trabajo: sueldo base semanal / horas base (48). */
CREATE FUNCTION fn_valor_hora(p_sueldo DECIMAL(10,2))
RETURNS DECIMAL(10,4)
READS SQL DATA
BEGIN
    DECLARE v_horas INT;

    SET v_horas = CAST((SELECT valor FROM configuracion
                        WHERE parametro = 'horas_semana_base') AS UNSIGNED);

    IF v_horas IS NULL OR v_horas = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Falta el parametro horas_semana_base en configuracion.';
    END IF;

    RETURN ROUND(p_sueldo / v_horas, 4);
END$$


/* fn_minutos_comida_extra
   Minutos que una comida se pasó del tiempo permitido (0 si no se pasó
   o si falta alguna de las dos checadas). */
CREATE FUNCTION fn_minutos_comida_extra(p_salida_comida DATETIME,
                                        p_regreso_comida DATETIME)
RETURNS INT
READS SQL DATA
BEGIN
    DECLARE v_comida INT;

    IF p_salida_comida IS NULL OR p_regreso_comida IS NULL THEN
        RETURN 0;
    END IF;

    SET v_comida = CAST((SELECT valor FROM configuracion
                         WHERE parametro = 'comida_min') AS UNSIGNED);

    RETURN GREATEST(TIMESTAMPDIFF(MINUTE, p_salida_comida, p_regreso_comida) - v_comida, 0);
END$$


/* fn_semana_cerrada
   1 si el empleado ya tiene el pago CERRADO de la semana que contiene
   esa fecha; 0 si no. La usan los triggers para proteger semanas pagadas. */
CREATE FUNCTION fn_semana_cerrada(p_id_empleado INT, p_fecha DATE)
RETURNS TINYINT
READS SQL DATA
BEGIN
    RETURN IF(EXISTS (SELECT 1
                      FROM pago_semanal p
                      INNER JOIN semana s ON s.id_semana = p.id_semana
                      WHERE p.id_empleado = p_id_empleado
                        AND p.cerrado = 1
                        AND p_fecha BETWEEN s.fecha_inicio AND s.fecha_fin), 1, 0);
END$$


/* =====================================================================
   PROCEDIMIENTOS ALMACENADOS
   ===================================================================== */

/* sp_registrar_checada
   Punto de entrada de la checada (página web, tablet o lector). Decide solo
   cuál de las 4 checadas sigue: entrada, salida a comida, regreso o salida.
   La clave personal se valida en la aplicación antes de llamar este procedimiento.
   Uso: CALL sp_registrar_checada(id_empleado, NOW(), 'Clave'); */
CREATE PROCEDURE sp_registrar_checada
(
    IN p_id_empleado INT,
    IN p_momento DATETIME,
    IN p_metodo VARCHAR(10)
)
BEGIN
    DECLARE v_fecha DATE;
    DECLARE v_id_registro INT;
    DECLARE v_en DATETIME;
    DECLARE v_sc DATETIME;
    DECLARE v_rc DATETIME;
    DECLARE v_sa DATETIME;
    DECLARE v_ultima DATETIME;

    IF NOT EXISTS (SELECT 1 FROM empleado
                   WHERE id_empleado = p_id_empleado AND activo = 1) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El empleado no existe o esta inactivo.';
    END IF;

    IF p_metodo IS NULL OR p_metodo NOT IN ('Clave', 'Huella') THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Metodo de checada no valido (use Clave o Huella).';
    END IF;

    SET v_fecha = DATE(p_momento);

    IF WEEKDAY(v_fecha) = 6 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'No se checa los domingos: no hay horario.';
    END IF;

    SET v_id_registro = (SELECT id_registro FROM registro_asistencia
                         WHERE id_empleado = p_id_empleado AND fecha = v_fecha);

    IF v_id_registro IS NULL THEN
        INSERT INTO registro_asistencia (id_empleado, fecha, entrada, metodo)
        VALUES (p_id_empleado, v_fecha, p_momento, p_metodo);

        SELECT 'ENTRADA' AS checada, p_momento AS hora;
    ELSE
        SELECT entrada, salida_comida, regreso_comida, salida
        INTO v_en, v_sc, v_rc, v_sa
        FROM registro_asistencia
        WHERE id_registro = v_id_registro;

        SET v_ultima = COALESCE(v_sa, v_rc, v_sc, v_en);

        IF v_ultima IS NOT NULL AND p_momento < v_ultima THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La hora es anterior a la checada previa del dia.';
        END IF;

        IF v_sc IS NULL THEN
            UPDATE registro_asistencia SET salida_comida = p_momento
            WHERE id_registro = v_id_registro;
            SELECT 'SALIDA_COMIDA' AS checada, p_momento AS hora;
        ELSEIF v_rc IS NULL THEN
            UPDATE registro_asistencia SET regreso_comida = p_momento
            WHERE id_registro = v_id_registro;
            SELECT 'REGRESO_COMIDA' AS checada, p_momento AS hora;
        ELSEIF v_sa IS NULL THEN
            UPDATE registro_asistencia SET salida = p_momento
            WHERE id_registro = v_id_registro;
            SELECT 'SALIDA' AS checada, p_momento AS hora;
        ELSE
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El empleado ya tiene las cuatro checadas de hoy.';
        END IF;
    END IF;
END$$


/* sp_generar_pago
   Calcula el pago de la semana de un empleado y lo guarda (pago_semanal +
   detalle_pago) DENTRO DE UNA TRANSACCION: si algo falla, se deshace todo
   con ROLLBACK. El sueldo base sale de la tabla empleado.

   Salario = sueldo base - descuentos + tiempo extra
   Valor de la hora = sueldo base / 48

   Uso: CALL sp_generar_pago(id_empleado, id_semana, id_usuario);
        CALL sp_ticket(id_empleado, id_semana);   -- para ver el ticket */
CREATE PROCEDURE sp_generar_pago
(
    IN p_id_empleado INT,
    IN p_id_semana INT,
    IN p_id_usuario INT
)
BEGIN
    DECLARE v_ini DATE;
    DECLARE v_fin DATE;
    DECLARE v_sueldo DECIMAL(10,2);
    DECLARE v_tolerancia INT;
    DECLARE v_umbral_1h INT;
    DECLARE v_umbral_2h INT;
    DECLARE v_umbral_extra INT;
    DECLARE v_mult_doble DECIMAL(4,2);
    DECLARE v_mult_triple DECIMAL(4,2);
    DECLARE v_valor_hora DECIMAL(10,4);
    DECLARE v_id_pago INT;
    DECLARE v_cerrado TINYINT;
    DECLARE v_min_retardo INT DEFAULT 0;
    DECLARE v_min_desc_retardo INT DEFAULT 0;
    DECLARE v_descuento DECIMAL(10,2);
    DECLARE v_extra DECIMAL(10,2);

    -- Si algo falla durante la transaccion: se deshace todo y se re-lanza el error.
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    -- Validaciones
    SET v_ini = (SELECT fecha_inicio FROM semana WHERE id_semana = p_id_semana);
    SET v_fin = (SELECT fecha_fin    FROM semana WHERE id_semana = p_id_semana);

    IF v_ini IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'La semana indicada no existe.';
    END IF;

    SET v_sueldo = (SELECT sueldo_base_semanal FROM empleado
                    WHERE id_empleado = p_id_empleado);

    IF v_sueldo IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El empleado indicado no existe.';
    END IF;

    -- Parametros
    SET v_tolerancia   = CAST((SELECT valor FROM configuracion WHERE parametro = 'tolerancia_min')      AS UNSIGNED);
    SET v_umbral_1h    = CAST((SELECT valor FROM configuracion WHERE parametro = 'retardo_min_para_1h') AS UNSIGNED);
    SET v_umbral_2h    = CAST((SELECT valor FROM configuracion WHERE parametro = 'retardo_min_para_2h') AS UNSIGNED);
    SET v_umbral_extra = CAST((SELECT valor FROM configuracion WHERE parametro = 'extra_umbral_min')    AS UNSIGNED);
    SET v_mult_doble   = CAST((SELECT valor FROM configuracion WHERE parametro = 'extra_mult_doble')    AS DECIMAL(4,2));
    SET v_mult_triple  = CAST((SELECT valor FROM configuracion WHERE parametro = 'extra_mult_triple')   AS DECIMAL(4,2));

    IF v_tolerancia IS NULL OR v_umbral_extra IS NULL
       OR v_mult_doble IS NULL OR v_mult_triple IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Faltan parametros de tolerancia o tiempo extra en configuracion.';
    END IF;

    SET v_valor_hora = fn_valor_hora(v_sueldo);

    SET v_id_pago = (SELECT id_pago FROM pago_semanal
                     WHERE id_empleado = p_id_empleado AND id_semana = p_id_semana);
    SET v_cerrado = IFNULL((SELECT cerrado FROM pago_semanal
                            WHERE id_empleado = p_id_empleado AND id_semana = p_id_semana), 0);

    IF v_cerrado = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago de esta semana ya esta cerrado y no se puede regenerar.';
    END IF;

    START TRANSACTION;

    IF v_id_pago IS NULL THEN
        INSERT INTO pago_semanal
            (id_empleado, id_semana, id_usuario, sueldo_base_usado, valor_hora, salario_final)
        VALUES
            (p_id_empleado, p_id_semana, p_id_usuario, v_sueldo, v_valor_hora, v_sueldo);

        SET v_id_pago = LAST_INSERT_ID();
    ELSE
        DELETE FROM detalle_pago WHERE id_pago = v_id_pago;

        UPDATE pago_semanal
        SET id_usuario = p_id_usuario,
            sueldo_base_usado = v_sueldo,
            valor_hora = v_valor_hora,
            fecha_generacion = NOW()
        WHERE id_pago = v_id_pago;
    END IF;

    -- RETARDOS (informativos; el descuento se decide con el acumulado semanal)
    INSERT INTO detalle_pago (id_pago, fecha, concepto, minutos, monto)
    SELECT v_id_pago, r.fecha, 'RETARDO',
           TIMESTAMPDIFF(MINUTE, TIMESTAMP(r.fecha, h.hora_entrada), r.entrada),
           0
    FROM registro_asistencia r
    INNER JOIN horario_general h ON h.dia_semana = WEEKDAY(r.fecha) + 1
    WHERE r.id_empleado = p_id_empleado
      AND r.fecha BETWEEN v_ini AND v_fin
      AND r.entrada IS NOT NULL
      AND r.entrada > TIMESTAMP(r.fecha, ADDTIME(h.hora_entrada, SEC_TO_TIME(v_tolerancia * 60)));

    -- COMIDA DE MAS (se descuenta)
    INSERT INTO detalle_pago (id_pago, fecha, concepto, minutos, monto)
    SELECT v_id_pago, t.fecha, 'COMIDA_EXCESO', t.minutos,
           -ROUND(t.minutos * v_valor_hora / 60, 2)
    FROM (
        SELECT r.fecha,
               fn_minutos_comida_extra(r.salida_comida, r.regreso_comida) AS minutos
        FROM registro_asistencia r
        WHERE r.id_empleado = p_id_empleado
          AND r.fecha BETWEEN v_ini AND v_fin
    ) t
    WHERE t.minutos > 0;

    -- SALIDA ANTES DE TIEMPO (se descuenta)
    INSERT INTO detalle_pago (id_pago, fecha, concepto, minutos, monto)
    SELECT v_id_pago, t.fecha, 'SALIDA_TEMPRANA', t.minutos,
           -ROUND(t.minutos * v_valor_hora / 60, 2)
    FROM (
        SELECT r.fecha,
               TIMESTAMPDIFF(MINUTE, r.salida, TIMESTAMP(r.fecha, h.hora_salida)) AS minutos
        FROM registro_asistencia r
        INNER JOIN horario_general h ON h.dia_semana = WEEKDAY(r.fecha) + 1
        WHERE r.id_empleado = p_id_empleado
          AND r.fecha BETWEEN v_ini AND v_fin
          AND r.salida IS NOT NULL
    ) t
    WHERE t.minutos > 0;

    -- FALTA INJUSTIFICADA (se descuenta solo la jornada de ese dia)
    INSERT INTO detalle_pago (id_pago, fecha, concepto, minutos, monto)
    SELECT v_id_pago, t.fecha, 'FALTA_INJUSTIFICADA', t.minutos,
           -ROUND(t.minutos * v_valor_hora / 60, 2)
    FROM (
        SELECT a.fecha,
               TIME_TO_SEC(TIMEDIFF(h.hora_salida, h.hora_entrada)) DIV 60 AS minutos
        FROM ausencia a
        INNER JOIN horario_general h ON h.dia_semana = WEEKDAY(a.fecha) + 1
        WHERE a.id_empleado = p_id_empleado
          AND a.fecha BETWEEN v_ini AND v_fin
          AND a.tipo = 'Injustificada'
    ) t;

    -- TIEMPO EXTRA (se paga): los primeros minutos de la semana con multiplicador
    -- doble y los que pasen del umbral con multiplicador triple.
    INSERT INTO detalle_pago (id_pago, fecha, concepto, minutos, monto)
    SELECT v_id_pago, x.fecha, 'TIEMPO_EXTRA', x.minutos,
           ROUND((x.dobles * v_mult_doble + (x.minutos - x.dobles) * v_mult_triple)
                 * v_valor_hora / 60, 2)
    FROM (
        SELECT t.fecha, t.minutos,
               GREATEST(0, LEAST(t.minutos,
                   v_umbral_extra - (SUM(t.minutos) OVER (ORDER BY t.fecha) - t.minutos))) AS dobles
        FROM (
            SELECT r.fecha,
                   TIMESTAMPDIFF(MINUTE, TIMESTAMP(r.fecha, h.hora_salida), r.salida) AS minutos
            FROM registro_asistencia r
            INNER JOIN horario_general h ON h.dia_semana = WEEKDAY(r.fecha) + 1
            WHERE r.id_empleado = p_id_empleado
              AND r.fecha BETWEEN v_ini AND v_fin
              AND r.salida IS NOT NULL
        ) t
        WHERE t.minutos > 0
    ) x;

    -- RETARDOS ACUMULADOS: solo se descuenta si el dueño ya definio los umbrales
    SET v_min_retardo = IFNULL((SELECT SUM(minutos) FROM detalle_pago
                                WHERE id_pago = v_id_pago AND concepto = 'RETARDO'), 0);

    IF v_umbral_2h IS NOT NULL AND v_min_retardo >= v_umbral_2h THEN
        SET v_min_desc_retardo = 120;
    ELSEIF v_umbral_1h IS NOT NULL AND v_min_retardo >= v_umbral_1h THEN
        SET v_min_desc_retardo = 60;
    END IF;

    IF v_min_desc_retardo > 0 THEN
        INSERT INTO detalle_pago (id_pago, fecha, concepto, minutos, monto)
        VALUES (v_id_pago, NULL, 'RETARDO_ACUMULADO', v_min_desc_retardo,
                -ROUND(v_min_desc_retardo * v_valor_hora / 60, 2));
    END IF;

    -- Totales (el descuento y el extra son la suma de los renglones del ticket)
    SET v_descuento = -1 * IFNULL((SELECT SUM(monto) FROM detalle_pago
                                   WHERE id_pago = v_id_pago AND monto < 0), 0);
    SET v_extra = IFNULL((SELECT SUM(monto) FROM detalle_pago
                          WHERE id_pago = v_id_pago AND concepto = 'TIEMPO_EXTRA'), 0);

    UPDATE pago_semanal
    SET min_retardo = v_min_retardo,
        min_salida_temprana = IFNULL((SELECT SUM(minutos) FROM detalle_pago
                                      WHERE id_pago = v_id_pago AND concepto = 'SALIDA_TEMPRANA'), 0),
        min_falta = IFNULL((SELECT SUM(minutos) FROM detalle_pago
                            WHERE id_pago = v_id_pago AND concepto = 'FALTA_INJUSTIFICADA'), 0),
        min_exceso_comida = IFNULL((SELECT SUM(minutos) FROM detalle_pago
                                    WHERE id_pago = v_id_pago AND concepto = 'COMIDA_EXCESO'), 0),
        min_extra = IFNULL((SELECT SUM(minutos) FROM detalle_pago
                            WHERE id_pago = v_id_pago AND concepto = 'TIEMPO_EXTRA'), 0),
        descuento_total = v_descuento,
        monto_extra = v_extra,
        salario_final = GREATEST(0, v_sueldo - v_descuento + v_extra)
    WHERE id_pago = v_id_pago;

    COMMIT;
END$$


/* sp_ticket
   Muestra el ticket de un empleado en una semana usando SOLO lo que quedó
   guardado en pago_semanal y detalle_pago (no recalcula).
   Devuelve dos resultados: 1) resumen, 2) detalle por dia. */
CREATE PROCEDURE sp_ticket
(
    IN p_id_empleado INT,
    IN p_id_semana INT
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pago_semanal
                   WHERE id_empleado = p_id_empleado AND id_semana = p_id_semana) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Ese pago todavia no se ha generado.';
    END IF;

    SELECT
        CONCAT(e.nombre, ' ', e.apellido_paterno,
               IFNULL(CONCAT(' ', e.apellido_materno), '')) AS empleado,
        pu.nombre AS puesto,
        s.fecha_inicio,
        s.fecha_fin,
        p.sueldo_base_usado AS sueldo_base,
        ROUND(p.valor_hora, 2) AS valor_hora,
        p.min_exceso_comida,
        p.min_retardo,
        p.min_salida_temprana,
        p.min_falta,
        p.min_extra,
        p.descuento_total,
        p.monto_extra,
        p.salario_final AS salario_a_pagar,
        IF(p.cerrado = 1, 'Cerrado', 'Borrador') AS estado,
        p.fecha_generacion,
        u.nombre_completo AS generado_por,
        p.observaciones
    FROM pago_semanal p
    INNER JOIN empleado e ON e.id_empleado = p.id_empleado
    INNER JOIN puesto pu ON pu.id_puesto = e.id_puesto
    INNER JOIN semana s ON s.id_semana = p.id_semana
    LEFT JOIN usuario u ON u.id_usuario = p.id_usuario
    WHERE p.id_empleado = p_id_empleado
      AND p.id_semana = p_id_semana;

    SELECT
        d.fecha,
        CASE d.concepto
            WHEN 'COMIDA_EXCESO'        THEN 'Comida de más'
            WHEN 'RETARDO'              THEN 'Retardo'
            WHEN 'RETARDO_ACUMULADO'    THEN 'Descuento por retardos acumulados'
            WHEN 'SALIDA_TEMPRANA'      THEN 'Salida antes de tiempo'
            WHEN 'FALTA_INJUSTIFICADA'  THEN 'Falta injustificada'
            WHEN 'TIEMPO_EXTRA'         THEN 'Tiempo extra'
        END AS concepto,
        d.minutos,
        d.monto
    FROM detalle_pago d
    INNER JOIN pago_semanal p ON p.id_pago = d.id_pago
    WHERE p.id_empleado = p_id_empleado
      AND p.id_semana = p_id_semana
    ORDER BY d.fecha IS NULL, d.fecha, d.concepto;
END$$


/* sp_cerrar_pago
   Marca el pago como cerrado: ya no se puede regenerar y el ticket queda
   fijo como evidencia de lo que se pagó. */
CREATE PROCEDURE sp_cerrar_pago
(
    IN p_id_pago INT
)
BEGIN
    DECLARE v_cerrado TINYINT;

    IF NOT EXISTS (SELECT 1 FROM pago_semanal WHERE id_pago = p_id_pago) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago indicado no existe.';
    END IF;

    SET v_cerrado = (SELECT cerrado FROM pago_semanal WHERE id_pago = p_id_pago);

    IF v_cerrado = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago ya estaba cerrado.';
    END IF;

    UPDATE pago_semanal
    SET cerrado = 1
    WHERE id_pago = p_id_pago;
END$$


/* sp_agregar_observacion
   El encargado escribe una nota que justifica el pago. Se permite aunque el
   pago esté cerrado, porque no cambia ningún monto. */
CREATE PROCEDURE sp_agregar_observacion
(
    IN p_id_pago INT,
    IN p_texto VARCHAR(255)
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pago_semanal WHERE id_pago = p_id_pago) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago indicado no existe.';
    END IF;

    UPDATE pago_semanal
    SET observaciones = p_texto
    WHERE id_pago = p_id_pago;
END$$


/* =====================================================================
   TRIGGERS
   Regla: lo que protege la integridad de los datos lo hace la BASE DE DATOS;
   la aplicación no repite esa operación (por ejemplo, no escribe la auditoría).
   ===================================================================== */

/* ---- Semana cerrada: no se pueden agregar, cambiar ni borrar checadas ---- */

-- Evento: BEFORE INSERT en registro_asistencia
-- Condición: el empleado ya tiene cerrado el pago de la semana de esa fecha
-- Efecto: rechaza la operación con un error
CREATE TRIGGER trg_registro_bi_cerrado
BEFORE INSERT ON registro_asistencia
FOR EACH ROW
BEGIN
    IF fn_semana_cerrada(NEW.id_empleado, NEW.fecha) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'La semana de esa checada ya esta cerrada y no se puede modificar.';
    END IF;
END$$

-- Evento: BEFORE UPDATE en registro_asistencia
-- Condición: la semana original o la nueva ya está cerrada
-- Efecto: rechaza la operación con un error
CREATE TRIGGER trg_registro_bu_cerrado
BEFORE UPDATE ON registro_asistencia
FOR EACH ROW
BEGIN
    IF fn_semana_cerrada(OLD.id_empleado, OLD.fecha) = 1
       OR fn_semana_cerrada(NEW.id_empleado, NEW.fecha) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'La semana de esa checada ya esta cerrada y no se puede modificar.';
    END IF;
END$$

-- Evento: BEFORE DELETE en registro_asistencia
-- Condición: la semana de esa checada ya está cerrada
-- Efecto: rechaza la operación con un error
CREATE TRIGGER trg_registro_bd_cerrado
BEFORE DELETE ON registro_asistencia
FOR EACH ROW
BEGIN
    IF fn_semana_cerrada(OLD.id_empleado, OLD.fecha) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'La semana de esa checada ya esta cerrada y no se puede modificar.';
    END IF;
END$$

/* ---- Auditoría de correcciones ---- */

-- Evento: AFTER UPDATE en registro_asistencia
-- Condición: se cambió una hora que YA tenía valor (una corrección).
--            Llenar una checada vacía (el uso normal) no se audita.
-- Efecto: guarda en auditoria_registro lo anterior, lo nuevo y quién lo hizo
CREATE TRIGGER trg_registro_au_auditoria
AFTER UPDATE ON registro_asistencia
FOR EACH ROW
BEGIN
    IF (OLD.entrada        IS NOT NULL AND NOT (OLD.entrada        <=> NEW.entrada))
    OR (OLD.salida_comida  IS NOT NULL AND NOT (OLD.salida_comida  <=> NEW.salida_comida))
    OR (OLD.regreso_comida IS NOT NULL AND NOT (OLD.regreso_comida <=> NEW.regreso_comida))
    OR (OLD.salida         IS NOT NULL AND NOT (OLD.salida         <=> NEW.salida)) THEN
        INSERT INTO auditoria_registro
            (id_registro, id_empleado, fecha,
             entrada_anterior, salida_comida_anterior, regreso_comida_anterior, salida_anterior,
             entrada_nueva, salida_comida_nueva, regreso_comida_nueva, salida_nueva,
             id_usuario_app, usuario_bd)
        VALUES
            (OLD.id_registro, OLD.id_empleado, OLD.fecha,
             OLD.entrada, OLD.salida_comida, OLD.regreso_comida, OLD.salida,
             NEW.entrada, NEW.salida_comida, NEW.regreso_comida, NEW.salida,
             NEW.modificado_por, CURRENT_USER());
    END IF;
END$$

/* ---- Pago cerrado: el ticket queda congelado ---- */

-- Evento: BEFORE INSERT en detalle_pago
-- Condición: el pago al que pertenece el renglón está cerrado
-- Efecto: rechaza la operación con un error
CREATE TRIGGER trg_detalle_bi_cerrado
BEFORE INSERT ON detalle_pago
FOR EACH ROW
BEGIN
    IF (SELECT cerrado FROM pago_semanal WHERE id_pago = NEW.id_pago) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago esta cerrado: su ticket no se puede modificar.';
    END IF;
END$$

-- Evento: BEFORE UPDATE en detalle_pago
-- Condición: el pago al que pertenece el renglón está cerrado
-- Efecto: rechaza la operación con un error
CREATE TRIGGER trg_detalle_bu_cerrado
BEFORE UPDATE ON detalle_pago
FOR EACH ROW
BEGIN
    IF (SELECT cerrado FROM pago_semanal WHERE id_pago = OLD.id_pago) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago esta cerrado: su ticket no se puede modificar.';
    END IF;
END$$

-- Evento: BEFORE DELETE en detalle_pago
-- Condición: el pago al que pertenece el renglón está cerrado
-- Efecto: rechaza la operación con un error
CREATE TRIGGER trg_detalle_bd_cerrado
BEFORE DELETE ON detalle_pago
FOR EACH ROW
BEGIN
    IF (SELECT cerrado FROM pago_semanal WHERE id_pago = OLD.id_pago) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago esta cerrado: su ticket no se puede modificar.';
    END IF;
END$$

-- Evento: BEFORE UPDATE en pago_semanal
-- Condición: el pago ya estaba cerrado y se intenta cambiar un monto o reabrirlo
--            (escribir observaciones sí se permite)
-- Efecto: rechaza la operación con un error
CREATE TRIGGER trg_pago_bu_cerrado
BEFORE UPDATE ON pago_semanal
FOR EACH ROW
BEGIN
    IF OLD.cerrado = 1
       AND (NOT (NEW.sueldo_base_usado <=> OLD.sueldo_base_usado)
         OR NOT (NEW.valor_hora        <=> OLD.valor_hora)
         OR NOT (NEW.descuento_total   <=> OLD.descuento_total)
         OR NOT (NEW.monto_extra       <=> OLD.monto_extra)
         OR NOT (NEW.salario_final     <=> OLD.salario_final)
         OR NEW.cerrado <> OLD.cerrado) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El pago esta cerrado: solo se pueden agregar observaciones.';
    END IF;
END$$

DELIMITER ;

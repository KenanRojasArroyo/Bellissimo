/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : 01_esquema.sql
   Motor   : MySQL 8.0 (MySQL Workbench)
   Orden   : 01_esquema -> 02_datos -> 03_rutinas -> 04_consultas_vistas
             -> 05_usuario_app (opcional) ; pruebas en ../Pruebas/pruebas_bd.sql

   ADVERTENCIA: este script comienza con DROP DATABASE IF EXISTS.
   Elimina y reconstruye la base local belissimo.

   Reglas del negocio (según la entrevista al dueño):
   - Horario único: lunes a viernes 9:00-17:30, sábado 9:00-15:30.
   - Tolerancia de 5 min; pasando esos minutos la entrada es retardo.
   - Comida de 30 min incluida en el pago; el exceso se descuenta.
   - El sueldo base se captura al registrar al empleado y se calcula
     sobre 48 horas a la semana.
   - Falta injustificada: se descuenta solo la jornada del día.
     Falta justificada (salud) y vacaciones: no se descuentan.
   - Retardos: se acumulan en la semana (regla de descuento PENDIENTE,
     configurable en la tabla configuracion).
   - Tiempo extra: lo calcula el sistema con el valor de la hora y un
     multiplicador configurable.
   - Cada pago queda "congelado" en detalle_pago para justificarlo con
     un ticket, y una semana cerrada ya no se puede modificar.
   ===================================================================== */

DROP DATABASE IF EXISTS belissimo;

CREATE DATABASE belissimo
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE belissimo;

/* ---------------------------------------------------------------------
   1. TABLAS
   --------------------------------------------------------------------- */

CREATE TABLE puesto
(
    id_puesto INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador del puesto',
    nombre VARCHAR(50) NOT NULL UNIQUE COMMENT 'Nombre del puesto (único)'
) COMMENT = 'Catálogo de puestos de trabajo';

CREATE TABLE empleado
(
    id_empleado INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador del empleado',
    id_puesto INT NOT NULL COMMENT 'Puesto que ocupa el empleado',
    nombre VARCHAR(60) NOT NULL COMMENT 'Nombre(s) del empleado',
    apellido_paterno VARCHAR(60) NOT NULL COMMENT 'Apellido paterno',
    apellido_materno VARCHAR(60) COMMENT 'Apellido materno (opcional)',
    telefono VARCHAR(20) COMMENT 'Teléfono de contacto',
    fecha_ingreso DATE NOT NULL COMMENT 'Fecha en que empezó a trabajar',
    sueldo_base_semanal DECIMAL(10,2) NOT NULL COMMENT 'Sueldo semanal pactado, base del cálculo (48 h)',
    clave_hash VARCHAR(255) NULL COMMENT 'Hash de la clave personal para checar (nunca texto plano)', -- clave personal para checar (HASH, nunca en texto plano)
    id_huella INT NULL UNIQUE COMMENT 'Identificador en el lector de huella (uso futuro)', -- identificador del lector de huella (se usará después)
    activo BOOLEAN NOT NULL DEFAULT 1 COMMENT '1 = trabaja actualmente, 0 = baja',

    CONSTRAINT fk_empleado_puesto
        FOREIGN KEY (id_puesto)
        REFERENCES puesto(id_puesto)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT ck_empleado_sueldo
        CHECK (sueldo_base_semanal > 0)
) COMMENT = 'Personas que checan asistencia y reciben pago semanal';

-- Personas que usan la página web (dueño, encargado).
CREATE TABLE usuario
(
    id_usuario INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador del usuario de la página',
    nombre_completo VARCHAR(120) NOT NULL COMMENT 'Nombre de la persona',
    nombre_usuario VARCHAR(50) NOT NULL UNIQUE COMMENT 'Nombre para iniciar sesión (único)',
    contrasena VARCHAR(255) NOT NULL COMMENT 'Hash de la contraseña (nunca texto plano)', -- guardar HASH, nunca contraseña en texto plano
    rol ENUM('Dueno', 'Encargado') NOT NULL DEFAULT 'Encargado' COMMENT 'Rol: Dueno (dueño) o Encargado',
    activo BOOLEAN NOT NULL DEFAULT 1 COMMENT '1 = puede entrar a la página, 0 = deshabilitado'
) COMMENT = 'Personas que usan la página web (dueño y encargado)';

-- dia_semana: 1 = lunes ... 6 = sábado (coincide con WEEKDAY(fecha) + 1)
CREATE TABLE horario_general
(
    dia_semana TINYINT PRIMARY KEY COMMENT 'Día: 1 = lunes ... 6 = sábado (WEEKDAY + 1)',
    hora_entrada TIME NOT NULL COMMENT 'Hora oficial de entrada',
    hora_salida TIME NOT NULL COMMENT 'Hora oficial de salida',

    CONSTRAINT ck_horario_dia
        CHECK (dia_semana BETWEEN 1 AND 6),

    CONSTRAINT ck_horario_horas
        CHECK (hora_salida > hora_entrada)
) COMMENT = 'Horario oficial por día de la semana (lunes a sábado)';

-- Parámetros que pueden cambiar sin modificar el sistema.
-- valor NULL = regla todavía sin definir por el dueño.
CREATE TABLE configuracion
(
    parametro VARCHAR(50) PRIMARY KEY COMMENT 'Nombre del parámetro',
    valor VARCHAR(50) NULL COMMENT 'Valor del parámetro; NULL = regla pendiente',
    descripcion VARCHAR(200) COMMENT 'Qué significa el parámetro'
) COMMENT = 'Parámetros del cálculo de pago editables sin cambiar el sistema';

-- Una fila por empleado y día con las cuatro checadas.
CREATE TABLE registro_asistencia
(
    id_registro INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador del registro diario',
    id_empleado INT NOT NULL COMMENT 'Empleado que checó',
    fecha DATE NOT NULL COMMENT 'Día laboral del registro',
    entrada DATETIME NULL COMMENT 'Checada 1: entrada',
    salida_comida DATETIME NULL COMMENT 'Checada 2: salida a comer',
    regreso_comida DATETIME NULL COMMENT 'Checada 3: regreso de comer',
    salida DATETIME NULL COMMENT 'Checada 4: salida',
    metodo ENUM('Clave', 'Huella', 'Manual') NOT NULL DEFAULT 'Clave' COMMENT 'Cómo se registró: Clave, Huella o Manual (corrección)',
    modificado_por INT NULL COMMENT 'Usuario que corrigió la checada a mano', -- usuario que corrigió una checada a mano

    CONSTRAINT fk_registro_empleado
        FOREIGN KEY (id_empleado)
        REFERENCES empleado(id_empleado)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_registro_usuario
        FOREIGN KEY (modificado_por)
        REFERENCES usuario(id_usuario)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT uq_registro_dia
        UNIQUE (id_empleado, fecha),

    CONSTRAINT ck_registro_fecha
        CHECK (entrada IS NULL OR DATE(entrada) = fecha),

    CONSTRAINT ck_registro_orden
        CHECK (
            (entrada IS NULL OR salida_comida IS NULL OR salida_comida >= entrada)
            AND (salida_comida IS NULL OR regreso_comida IS NULL OR regreso_comida >= salida_comida)
            AND (regreso_comida IS NULL OR salida IS NULL OR salida >= regreso_comida)
        )
) COMMENT = 'Las cuatro checadas de un empleado en un día';

CREATE TABLE ausencia
(
    id_ausencia INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador de la ausencia',
    id_empleado INT NOT NULL COMMENT 'Empleado que faltó',
    fecha DATE NOT NULL COMMENT 'Día de la ausencia',
    tipo ENUM('Justificada', 'Injustificada', 'Vacaciones') NOT NULL COMMENT 'Justificada (salud), Injustificada o Vacaciones',
    observaciones VARCHAR(200) COMMENT 'Motivo o nota',

    CONSTRAINT fk_ausencia_empleado
        FOREIGN KEY (id_empleado)
        REFERENCES empleado(id_empleado)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT uq_ausencia_dia
        UNIQUE (id_empleado, fecha)
) COMMENT = 'Días que un empleado no asistió y su tipo';

CREATE TABLE semana
(
    id_semana INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador de la semana de pago',
    fecha_inicio DATE NOT NULL UNIQUE COMMENT 'Lunes de la semana (único)',
    fecha_fin DATE NOT NULL COMMENT 'Sábado de la semana',

    CONSTRAINT ck_semana_fechas
        CHECK (fecha_fin > fecha_inicio)
) COMMENT = 'Periodo de pago de lunes a sábado';

-- Resumen del pago de un empleado en una semana (una fila por empleado y semana).
-- Los minutos y montos son una FOTO de lo que se calculó (ver detalle_pago).
CREATE TABLE pago_semanal
(
    id_pago INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador del pago',
    id_empleado INT NOT NULL COMMENT 'Empleado al que se paga',
    id_semana INT NOT NULL COMMENT 'Semana que se paga',
    id_usuario INT NULL COMMENT 'Encargado que generó el pago', -- encargado que generó el ticket
    sueldo_base_usado DECIMAL(10,2) NOT NULL COMMENT 'Copia del sueldo base al momento de calcular',
    valor_hora DECIMAL(10,4) NOT NULL COMMENT 'Sueldo base / 48 al momento de calcular',
    min_retardo INT NOT NULL DEFAULT 0 COMMENT 'Minutos de retardo de la semana',
    min_salida_temprana INT NOT NULL DEFAULT 0 COMMENT 'Minutos de salida antes de hora',
    min_falta INT NOT NULL DEFAULT 0 COMMENT 'Minutos de faltas injustificadas',
    min_exceso_comida INT NOT NULL DEFAULT 0 COMMENT 'Minutos de comida de más',
    min_extra INT NOT NULL DEFAULT 0 COMMENT 'Minutos de tiempo extra',
    descuento_total DECIMAL(10,2) NOT NULL DEFAULT 0 COMMENT 'Suma de renglones con monto negativo (positivo)',
    monto_extra DECIMAL(10,2) NOT NULL DEFAULT 0 COMMENT 'Suma pagada por tiempo extra',
    salario_final DECIMAL(10,2) NOT NULL COMMENT 'Sueldo base - descuentos + extra, mínimo 0',
    cerrado BOOLEAN NOT NULL DEFAULT 0 COMMENT '1 = pagado y congelado, 0 = borrador', -- 1 = pago pagado, ya no se modifica
    fecha_generacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Cuándo se calculó por última vez',
    observaciones VARCHAR(255) COMMENT 'Nota del encargado (editable aun cerrado)',

    CONSTRAINT fk_pago_empleado
        FOREIGN KEY (id_empleado)
        REFERENCES empleado(id_empleado)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_pago_semana
        FOREIGN KEY (id_semana)
        REFERENCES semana(id_semana)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT fk_pago_usuario
        FOREIGN KEY (id_usuario)
        REFERENCES usuario(id_usuario)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT uq_pago_empleado_semana
        UNIQUE (id_empleado, id_semana),

    CONSTRAINT ck_pago_montos
        CHECK (sueldo_base_usado > 0 AND descuento_total >= 0
               AND monto_extra >= 0 AND salario_final >= 0)
) COMMENT = 'Resumen congelado del pago de un empleado en una semana';

-- Cada renglón del ticket. Se guarda tal como se calculó para poder
-- justificar el pago aunque después se corrija una checada.
-- monto: negativo = descuento, 0 = informativo, positivo = pago extra.
CREATE TABLE detalle_pago
(
    id_detalle INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador del renglón',
    id_pago INT NOT NULL COMMENT 'Pago al que pertenece el renglón',
    fecha DATE NULL COMMENT 'Día del evento; NULL en conceptos semanales',
    concepto ENUM(
        'COMIDA_EXCESO',
        'RETARDO',
        'RETARDO_ACUMULADO',
        'SALIDA_TEMPRANA',
        'FALTA_INJUSTIFICADA',
        'TIEMPO_EXTRA'
    ) NOT NULL COMMENT 'Tipo de renglón del ticket',
    minutos INT NOT NULL DEFAULT 0 COMMENT 'Minutos del evento',
    monto DECIMAL(10,2) NOT NULL DEFAULT 0 COMMENT 'Negativo = descuento, 0 = informativo, positivo = pago',

    CONSTRAINT fk_detalle_pago
        FOREIGN KEY (id_pago)
        REFERENCES pago_semanal(id_pago)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
) COMMENT = 'Renglones del ticket de pago tal como se calcularon';

-- Auditoría de correcciones hechas a las checadas (la llena un trigger).
-- No lleva llave foránea a registro_asistencia: debe sobrevivir si el registro se borra.
CREATE TABLE auditoria_registro
(
    id_auditoria INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador del cambio auditado',
    id_registro INT NOT NULL COMMENT 'Registro corregido (sin FK: sobrevive a borrados)',
    id_empleado INT NOT NULL COMMENT 'Empleado del registro corregido',
    fecha DATE NOT NULL COMMENT 'Día del registro corregido',
    entrada_anterior DATETIME NULL COMMENT 'Entrada antes del cambio',
    salida_comida_anterior DATETIME NULL COMMENT 'Salida a comer antes del cambio',
    regreso_comida_anterior DATETIME NULL COMMENT 'Regreso de comer antes del cambio',
    salida_anterior DATETIME NULL COMMENT 'Salida antes del cambio',
    entrada_nueva DATETIME NULL COMMENT 'Entrada después del cambio',
    salida_comida_nueva DATETIME NULL COMMENT 'Salida a comer después del cambio',
    regreso_comida_nueva DATETIME NULL COMMENT 'Regreso de comer después del cambio',
    salida_nueva DATETIME NULL COMMENT 'Salida después del cambio',
    id_usuario_app INT NULL COMMENT 'Usuario de la página que corrigió (modificado_por)',
    usuario_bd VARCHAR(100) NOT NULL COMMENT 'Usuario de MySQL que ejecutó el cambio',
    fecha_cambio DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Momento del cambio'
) COMMENT = 'Bitácora de correcciones a checadas (la llena un trigger)';

/* ---------------------------------------------------------------------
   1b. ÍNDICES PARA BÚSQUEDAS FRECUENTES
   Las PK, los UNIQUE y las FK ya crean su propio índice. Estos cubren
   búsquedas que no empiezan por esas columnas.
   --------------------------------------------------------------------- */

-- Reporte de asistencia de un día para todos los empleados
-- (uq_registro_dia empieza por id_empleado y no sirve para buscar solo por fecha).
CREATE INDEX idx_registro_fecha ON registro_asistencia (fecha);

-- Ausencias de un día o rango de fechas de todo el personal.
CREATE INDEX idx_ausencia_fecha ON ausencia (fecha);

-- sp_generar_pago suma los renglones de un pago por concepto.
CREATE INDEX idx_detalle_pago_concepto ON detalle_pago (id_pago, concepto);

-- Buscador de empleados por apellido y nombre en la página.
CREATE INDEX idx_empleado_nombre ON empleado (apellido_paterno, nombre);

-- Historial de correcciones de una checada.
CREATE INDEX idx_auditoria_registro ON auditoria_registro (id_registro);

/* ---------------------------------------------------------------------
   2. CATÁLOGOS NECESARIOS PARA QUE EL SISTEMA FUNCIONE
   --------------------------------------------------------------------- */

INSERT INTO puesto (nombre) VALUES
('Recepcionista'),
('Repartidor'),
('Florista');

INSERT INTO horario_general (dia_semana, hora_entrada, hora_salida) VALUES
(1, '09:00:00', '17:30:00'),
(2, '09:00:00', '17:30:00'),
(3, '09:00:00', '17:30:00'),
(4, '09:00:00', '17:30:00'),
(5, '09:00:00', '17:30:00'),
(6, '09:00:00', '15:30:00');

INSERT INTO configuracion (parametro, valor, descripcion) VALUES
('horas_semana_base',   '48', 'Horas a la semana sobre las que se calcula el valor de la hora'),
('tolerancia_min',      '5',  'Minutos de tolerancia en la entrada antes de contar retardo'),
('comida_min',          '30', 'Minutos de comida incluidos en el pago'),
('extra_umbral_min',    '540','Minutos extra de la semana que se pagan con el multiplicador doble (9 horas). Valor inicial según la LFT: confirmar con el contador'),
('extra_mult_doble',    '2',  'Multiplicador del valor de la hora para las primeras horas extra. Confirmar con el contador'),
('extra_mult_triple',   '3',  'Multiplicador para las horas extra que pasan del umbral. Confirmar con el contador'),
('retardo_min_para_1h', NULL, 'PENDIENTE: minutos de retardo acumulados en la semana para descontar 1 hora'),
('retardo_min_para_2h', NULL, 'PENDIENTE: minutos de retardo acumulados en la semana para descontar 2 horas');

/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : 01_esquema.sql
   Motor   : MySQL 8.0 (MySQL Workbench)
   Orden   : 01_esquema -> 02_datos -> 03_rutinas -> 04_consultas_vistas -> 05_pruebas

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
    id_puesto INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE empleado
(
    id_empleado INT AUTO_INCREMENT PRIMARY KEY,
    id_puesto INT NOT NULL,
    nombre VARCHAR(60) NOT NULL,
    apellido_paterno VARCHAR(60) NOT NULL,
    apellido_materno VARCHAR(60),
    telefono VARCHAR(20),
    fecha_ingreso DATE NOT NULL,
    sueldo_base_semanal DECIMAL(10,2) NOT NULL,
    clave_hash VARCHAR(255) NULL,   -- clave personal para checar (HASH, nunca en texto plano)
    id_huella INT NULL UNIQUE,      -- identificador del lector de huella (se usará después)
    activo TINYINT(1) NOT NULL DEFAULT 1,

    CONSTRAINT fk_empleado_puesto
        FOREIGN KEY (id_puesto)
        REFERENCES puesto(id_puesto)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT ck_empleado_sueldo
        CHECK (sueldo_base_semanal > 0)
);

-- Personas que usan la página web (dueño, encargado).
CREATE TABLE usuario
(
    id_usuario INT AUTO_INCREMENT PRIMARY KEY,
    nombre_completo VARCHAR(120) NOT NULL,
    nombre_usuario VARCHAR(50) NOT NULL UNIQUE,
    contrasena VARCHAR(255) NOT NULL, -- guardar HASH, nunca contraseña en texto plano
    rol ENUM('Dueno', 'Encargado') NOT NULL DEFAULT 'Encargado',
    activo TINYINT(1) NOT NULL DEFAULT 1
);

-- dia_semana: 1 = lunes ... 6 = sábado (coincide con WEEKDAY(fecha) + 1)
CREATE TABLE horario_general
(
    dia_semana TINYINT PRIMARY KEY,
    hora_entrada TIME NOT NULL,
    hora_salida TIME NOT NULL,

    CONSTRAINT ck_horario_dia
        CHECK (dia_semana BETWEEN 1 AND 6),

    CONSTRAINT ck_horario_horas
        CHECK (hora_salida > hora_entrada)
);

-- Parámetros que pueden cambiar sin modificar el sistema.
-- valor NULL = regla todavía sin definir por el dueño.
CREATE TABLE configuracion
(
    parametro VARCHAR(50) PRIMARY KEY,
    valor VARCHAR(50) NULL,
    descripcion VARCHAR(200)
);

-- Una fila por empleado y día con las cuatro checadas.
CREATE TABLE registro_asistencia
(
    id_registro INT AUTO_INCREMENT PRIMARY KEY,
    id_empleado INT NOT NULL,
    fecha DATE NOT NULL,
    entrada DATETIME NULL,
    salida_comida DATETIME NULL,
    regreso_comida DATETIME NULL,
    salida DATETIME NULL,
    metodo ENUM('Clave', 'Huella', 'Manual') NOT NULL DEFAULT 'Clave',
    modificado_por INT NULL, -- usuario que corrigió una checada a mano

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
);

CREATE TABLE ausencia
(
    id_ausencia INT AUTO_INCREMENT PRIMARY KEY,
    id_empleado INT NOT NULL,
    fecha DATE NOT NULL,
    tipo ENUM('Justificada', 'Injustificada', 'Vacaciones') NOT NULL,
    observaciones VARCHAR(200),

    CONSTRAINT fk_ausencia_empleado
        FOREIGN KEY (id_empleado)
        REFERENCES empleado(id_empleado)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT uq_ausencia_dia
        UNIQUE (id_empleado, fecha)
);

CREATE TABLE semana
(
    id_semana INT AUTO_INCREMENT PRIMARY KEY,
    fecha_inicio DATE NOT NULL UNIQUE,
    fecha_fin DATE NOT NULL,

    CONSTRAINT ck_semana_fechas
        CHECK (fecha_fin > fecha_inicio)
);

-- Resumen del pago de un empleado en una semana (una fila por empleado y semana).
-- Los minutos y montos son una FOTO de lo que se calculó (ver detalle_pago).
CREATE TABLE pago_semanal
(
    id_pago INT AUTO_INCREMENT PRIMARY KEY,
    id_empleado INT NOT NULL,
    id_semana INT NOT NULL,
    id_usuario INT NULL, -- encargado que generó el ticket
    sueldo_base_usado DECIMAL(10,2) NOT NULL,
    valor_hora DECIMAL(10,4) NOT NULL,
    min_retardo INT NOT NULL DEFAULT 0,
    min_salida_temprana INT NOT NULL DEFAULT 0,
    min_falta INT NOT NULL DEFAULT 0,
    min_exceso_comida INT NOT NULL DEFAULT 0,
    min_extra INT NOT NULL DEFAULT 0,
    descuento_total DECIMAL(10,2) NOT NULL DEFAULT 0,
    monto_extra DECIMAL(10,2) NOT NULL DEFAULT 0,
    salario_final DECIMAL(10,2) NOT NULL,
    cerrado TINYINT(1) NOT NULL DEFAULT 0, -- 1 = pago pagado, ya no se modifica
    fecha_generacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    observaciones VARCHAR(255),

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
);

-- Cada renglón del ticket. Se guarda tal como se calculó para poder
-- justificar el pago aunque después se corrija una checada.
-- monto: negativo = descuento, 0 = informativo, positivo = pago extra.
CREATE TABLE detalle_pago
(
    id_detalle INT AUTO_INCREMENT PRIMARY KEY,
    id_pago INT NOT NULL,
    fecha DATE NULL,
    concepto ENUM(
        'COMIDA_EXCESO',
        'RETARDO',
        'RETARDO_ACUMULADO',
        'SALIDA_TEMPRANA',
        'FALTA_INJUSTIFICADA',
        'TIEMPO_EXTRA'
    ) NOT NULL,
    minutos INT NOT NULL DEFAULT 0,
    monto DECIMAL(10,2) NOT NULL DEFAULT 0,

    CONSTRAINT fk_detalle_pago
        FOREIGN KEY (id_pago)
        REFERENCES pago_semanal(id_pago)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);

-- Auditoría de correcciones hechas a las checadas (la llena un trigger).
-- No lleva llave foránea a registro_asistencia: debe sobrevivir si el registro se borra.
CREATE TABLE auditoria_registro
(
    id_auditoria INT AUTO_INCREMENT PRIMARY KEY,
    id_registro INT NOT NULL,
    id_empleado INT NOT NULL,
    fecha DATE NOT NULL,
    entrada_anterior DATETIME NULL,
    salida_comida_anterior DATETIME NULL,
    regreso_comida_anterior DATETIME NULL,
    salida_anterior DATETIME NULL,
    entrada_nueva DATETIME NULL,
    salida_comida_nueva DATETIME NULL,
    regreso_comida_nueva DATETIME NULL,
    salida_nueva DATETIME NULL,
    id_usuario_app INT NULL,
    usuario_bd VARCHAR(100) NOT NULL,
    fecha_cambio DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

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

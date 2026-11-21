/* =====================================================================
   FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
   Archivo : 05_usuario_app.sql
   Motor   : MySQL 8.0

   Crea el usuario de MySQL que usa la página web, con el MÍNIMO de
   privilegios. Ejecutar como root DESPUÉS de 01 -> 04.

   ANTES DE EJECUTAR: reemplaza CAMBIAR_ESTA_CONTRASENA por una contraseña
   real y segura SOLO en tu copia local. NUNCA subas a GitHub el archivo con
   la contraseña real. La aplicación la lee de user-secrets o de una variable
   de entorno (ver Sql/conexion_plantilla.md).

   Por qué alcanza con estos permisos:
   - Los procedimientos, funciones, vistas y triggers se ejecutan con los
     permisos de su DEFINER (quien creó la base). Por eso la app puede generar
     pagos y la auditoría se escribe sola, sin que la app tenga permiso de
     escribir en pago_semanal, detalle_pago ni auditoria_registro.
   - No tiene DROP, ALTER, CREATE, INDEX, TRIGGER, GRANT ni acceso a otras bases.
   ===================================================================== */

-- Si el usuario ya existía, se recrea para que no conserve permisos viejos.
DROP USER IF EXISTS 'app_floreria'@'localhost';

CREATE USER 'app_floreria'@'localhost'
    IDENTIFIED BY 'CAMBIAR_ESTA_CONTRASENA'
    PASSWORD EXPIRE NEVER;

-- Rutinas: CALL sp_* y SELECT fn_*
GRANT EXECUTE ON belissimo.* TO 'app_floreria'@'localhost';

-- Vistas (solo lectura)
GRANT SELECT ON belissimo.v_asistencia_diaria    TO 'app_floreria'@'localhost';
GRANT SELECT ON belissimo.v_resumen_pago_semanal TO 'app_floreria'@'localhost';
GRANT SELECT ON belissimo.v_dias_sin_checada     TO 'app_floreria'@'localhost';

-- Catálogos y personas (CRUD). DELETE de empleado solo prospera si no tiene
-- historial (las FK con ON DELETE RESTRICT lo impiden); lo normal es activo = 0.
GRANT SELECT, INSERT, UPDATE         ON belissimo.puesto          TO 'app_floreria'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON belissimo.empleado        TO 'app_floreria'@'localhost';
GRANT SELECT, INSERT, UPDATE         ON belissimo.usuario         TO 'app_floreria'@'localhost';
GRANT SELECT, UPDATE                 ON belissimo.horario_general TO 'app_floreria'@'localhost';
GRANT SELECT, UPDATE                 ON belissimo.configuracion   TO 'app_floreria'@'localhost';
GRANT SELECT, INSERT                 ON belissimo.semana          TO 'app_floreria'@'localhost';

-- Asistencia: la checada normal entra por sp_registrar_checada; INSERT/UPDATE/DELETE
-- directos son para correcciones manuales del encargado (quedan auditadas y los
-- triggers bloquean las semanas cerradas).
GRANT SELECT, INSERT, UPDATE, DELETE ON belissimo.registro_asistencia TO 'app_floreria'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON belissimo.ausencia            TO 'app_floreria'@'localhost';

-- Pagos y auditoría: SOLO lectura. Se escriben únicamente con sp_generar_pago,
-- sp_cerrar_pago, sp_agregar_observacion y el trigger de auditoría.
GRANT SELECT ON belissimo.pago_semanal       TO 'app_floreria'@'localhost';
GRANT SELECT ON belissimo.detalle_pago       TO 'app_floreria'@'localhost';
GRANT SELECT ON belissimo.auditoria_registro TO 'app_floreria'@'localhost';

SHOW GRANTS FOR 'app_floreria'@'localhost';

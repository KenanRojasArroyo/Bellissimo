# Funcionalidad Web pendiente

La base de datos ya contiene las tablas, procedimientos y vistas de estos módulos
(ver `Sql/CONTRATO_BD.md`). Lo pendiente es conectar su funcionalidad en la aplicación Web
a través de Negocios y Datos.

- Registro de entradas y salidas (checador) con `sp_registrar_checada`
  — las vistas `RegistroEntradas` y `RegistroSalidas` existen, pero todavía no llaman a Negocios
- Pagos semanales y ticket con `sp_generar_pago`, `sp_ticket`, `sp_cerrar_pago`
  y `v_resumen_pago_semanal` — la página `Pagos` existe sin funcionalidad
- Ausencias
- Corrección manual de checadas (auditada por trigger)
- Reporte de asistencia diaria (`v_asistencia_diaria`)
- Aviso de días sin checada (`v_dias_sin_checada`)
- Usuarios / inicio de sesión (dueño y encargado) con contraseña en hash
- Configuración de parámetros y horario
- Catálogo de puestos (alta y edición)

## Mejoras técnicas futuras

- Filtro de empleados activos / inactivos en el listado de Personal
- Captura de la fecha de ingreso en Nuevo (hoy se usa la fecha del servidor)
- Traducir el error 1451 de MySQL a `ReglaNegocioException` si alguien registra historial
  entre la validación y el `DELETE`
- ILogger
- async / await
- Inyección de dependencias
- Interfaces
- ViewModels
- Pruebas automatizadas
- Deployment

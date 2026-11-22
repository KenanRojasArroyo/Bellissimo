# Funcionalidad pendiente

## Páginas que faltan (Frontend)

Negocios y Datos ya están listos y probados para estos módulos
(ver `Docs/PRUEBAS_BACKEND.md`); falta conectar las páginas:

- Checador: `RegistroEntradas` y `RegistroSalidas` todavía no llaman a `ChecadorNegocio`
- Asignar la clave del checador desde Personal (`PersonalNegocio.AsignarClave`)
- Ausencias (CRUD)
- Correcciones de checadas (CRUD) y reporte de asistencia diaria
- Puestos (alta y edición)
- Semanas, pagos, ticket, cierre, observaciones y aviso de días sin checada

## Fuera del alcance actual

- Inicio de sesión de dueño y encargado (contraseña en hash) y permisos por rol
- Configuración de parámetros del cálculo y del horario
- Lector de huella (`empleado.id_huella`, método `Huella`)

## Mejoras técnicas futuras

- Filtro de empleados activos / inactivos en el listado de Personal
- Captura de la fecha de ingreso en Nuevo (hoy se usa la fecha del servidor)
- ILogger
- async / await
- Inyección de dependencias
- Interfaces
- ViewModels
- Pruebas automatizadas en el repositorio
- Deployment

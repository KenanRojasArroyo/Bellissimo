# Pruebas Frontend

Rama: `frontend`. Base de datos reconstruida con los scripts iniciales y datos de prueba de **Bellissimo**.
Las pruebas de validación no dejan registros nuevos en la base: sólo se usan formularios que la validación rechaza y se verifican los mensajes en pantalla.

| No. | Prueba | Resultado |
| ---: | --- | :---: |
| 1 | Navegación principal | APROBADO |
| 2 | Listado y búsqueda de Personal | APROBADO |
| 3 | Validación de campos obligatorios (Nuevo Personal) | APROBADO |
| 4 | Validación de puesto y sueldo base | APROBADO |
| 5 | Edición de empleado | APROBADO |
| 6 | Detalle de empleado | APROBADO |
| 7 | Confirmación y bloqueo en Eliminar | APROBADO |
| 8 | Responsive 390 px (móvil) | APROBADO |
| 9 | Responsive 768 px (tablet) | APROBADO |
| 10 | Responsive 1366 px (escritorio) | APROBADO |

## Detalle de la revisión

1. **Navegación (F-01):** se revisaron los enlaces internos (Inicio, Ajustes de Personal, Nuevo empleado, Ver, Editar, Eliminar, Regresar y Cancelar) y ninguno responde 404 ni 500. Los módulos futuros en el menú principal permanecen identificados como pendientes / próximamente.
2. **Listado y búsqueda de Personal (F-02):** el listado muestra los empleados registrados en MySQL con su puesto real y estado (`Activo` / `Inactivo`); al buscar por nombre o puesto (por ejemplo `Ana`) la tabla filtra correctamente los resultados y el botón **Limpiar** restablece la vista.
3. **Validación de campos obligatorios (F-03):** al intentar guardar `Nuevo empleado` con el formulario vacío, no se envía a la base de datos y aparecen los mensajes visibles: `"El nombre es obligatorio."`, `"El apellido paterno es obligatorio."` y `"El teléfono es obligatorio."`.
4. **Validación de puesto y sueldo base (F-03):** al dejar el selector en `"Seleccione un puesto"` (`0`) o capturar un sueldo igual o menor a `0`, el sistema rechaza el registro mostrando `"Seleccione un puesto válido."` y `"El sueldo base semanal debe ser mayor a 0."`.
5. **Edición de empleado (F-04 / F-04A):** `Editar.cshtml` carga los datos actuales del empleado seleccionado, conserva su `IdPersonal` oculto, permite actualizar datos o cambiar el interruptor de estado (`Activo`) y redirige a `Index` mostrando el mensaje de confirmación en `TempData["Mensaje"]`.
6. **Detalle de empleado (F-07):** `Detalle.cshtml` muestra nombre completo, teléfono, puesto (`NombrePuesto` como texto legible), sueldo base semanal en formato moneda (`ToString("C")`), insignia de estado y fecha de registro (`dd/MM/yyyy`). No expone nombres de clases ni errores técnicos.
7. **Eliminar y manejo de errores (F-08):** `Eliminar.cshtml` muestra una alerta de confirmación con los datos del empleado antes de borrar. Cuando `PersonalNegocio.Eliminar` lanza `ReglaNegocioException` (empleado con registros asociados) o ocurre un fallo de conexión, se muestra la alerta roja con `Model.Error`, se oculta el formulario/botón **Eliminar** y permanece únicamente el botón **Regresar**.
8. **Responsive (F-09):** a `390 px`, `768 px` y `1366 px` ninguna página (`Inicio`, `Personal/Index`, `Personal/Nuevo`, `Personal/Editar`, `Personal/Detalle`, `Personal/Eliminar`) presenta desbordamiento horizontal; las tablas utilizan `table-responsive` y los formularios se adaptan mediante rejilla Bootstrap (`col-md-6`).

## Correcciones realizadas

- **Manejo visible de errores de carga (`F-04A`):** en `Pages/Personal/Editar.cshtml` y `Pages/Personal/Detalle.cshtml` se envolvió el formulario y la tabla de datos dentro de un bloque `else` respecto a `@if (!string.IsNullOrWhiteSpace(Model.Error))`, agregando un botón **Regresar** cuando ocurre un error de consulta para evitar mostrar formularios o tablas vacías.
- **Limpieza de directivas innecesarias (`RZ0005`):** se eliminó la directiva redundante `@using Bellissimo.Entidades` en `Index.cshtml`, `Editar.cshtml`, `Detalle.cshtml` y `Eliminar.cshtml` para mantener la compilación limpia de advertencias.
- **Confirmación de eliminación (`F-08`):** se implementó `Pages/Personal/Eliminar.cshtml` y `Eliminar.cshtml.cs` ocultando el botón de confirmación cuando existe un mensaje de error en `Model.Error`.
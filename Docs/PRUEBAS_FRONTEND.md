# Pruebas Funcionales e Integración de Frontend (`Bellissimo.Web`)

**Proyecto:** Bellissimo — Reloj Checador y Pago Semanal  
**Capa:** Presentación (`Bellissimo.Web` — ASP.NET Core Razor Pages)  
**Integración:** `Bellissimo.Negocios`, `Bellissimo.Entidades`, `Bellissimo.Datos` y MySQL 8.0 (`belissimo`)  
**Fecha de actualización:** Noviembre 2026

---

## 1. Configuración del Entorno de Pruebas

1. **Base de datos MySQL 8.0:**
   * Scripts ejecutados en orden: `01_esquema.sql`, `02_datos.sql`, `03_rutinas.sql`, `04_consultas_vistas.sql` y `05_usuario_app.sql`.
   * Usuario de aplicación configurado: `'app_floreria'@'localhost'` con privilegios mínimos sobre tablas, vistas y procedimientos almacenados (`sp_registrar_checada`, `sp_generar_pago`, `sp_cerrar_pago`, `sp_ticket`, `sp_agregar_observacion`).
2. **Configuración Web (`Bellissimo.Web`):**
   * Cadena de conexión `ConnectionStrings:Bellissimo` configurada localmente sin exponer credenciales en el repositorio.
   * Compilación verificada sin errores mediante `dotnet build`.

---

## 2. Matriz de Pruebas por Módulo

### 2.1. Módulo de Personal (`/Personal`)

| ID | Caso de Prueba | Acción / Entrada | Resultado Esperado | Estado |
|----|----------------|------------------|--------------------|--------|
| PER-01 | Listado y búsqueda de personal | Abrir `/Personal/Index` y filtrar por nombre o puesto. | Muestra el número de empleado (`#IdPersonal`), nombre completo, teléfono, puesto, estado de clave (`Asignada` / `Sin clave`) y estado (`Activo` / `Inactivo`). | Aprobado |
| PER-02 | Alta y edición de empleado | Registrar o modificar datos en `/Personal/Nuevo` y `/Personal/Editar/{id}` (nombre, apellidos, teléfono, puesto válido, sueldo > 0). | Guarda en MySQL y redirige a `/Personal/Index` mostrando alerta de confirmación. Valida campos obligatorios y rangos. | Aprobado |
| PER-03 | Asignación de clave del checador | En `/Personal/Editar/{id}`, capturar clave numérica de 4 a 8 dígitos y enviar formulario `AsignarClave`. | Guarda el hash de la clave (`ClaveHasher`), actualiza la insignia a `Clave asignada` y muestra mensaje de éxito. | Aprobado |
| PER-04 | Validación de formato de clave | Intentar asignar clave menor a 4 dígitos, mayor a 8 o con letras. | Rechaza la operación con el mensaje `"La clave debe tener de 4 a 8 dígitos."` sin sobrescribir el hash existente. | Aprobado |
| PER-05 | Protección ante eliminación con historial | Intentar eliminar desde `/Personal/Eliminar/{id}` un empleado con checadas, ausencias o pagos previos. | Bloquea el borrado físico (`ReglaNegocioException`) e indica desactivar al empleado desde `Editar` (baja lógica). | Aprobado |

---

### 2.2. Módulo Checador (`/RegistroEntradas` y `/RegistroSalidas`)

| ID | Caso de Prueba | Acción / Entrada | Resultado Esperado | Estado |
|----|----------------|------------------|--------------------|--------|
| CHE-01 | Credenciales de checador | Ingresar número de empleado (`IdEmpleado`) y clave numérica (4–8 dígitos) en Entrada, Salida a Comer, Regreso de Comer o Salida del Turno. | Valida existencia, estado activo y hash de clave mediante `ChecadorNegocio.Registrar`. | Aprobado |
| CHE-02 | Empleado sin clave asignada | Intentar checar con un empleado cuyo campo `TieneClave` es falso. | Muestra alerta `"El empleado no tiene clave asignada. Pida al encargado que la registre en Personal."` | Aprobado |
| CHE-03 | Credenciales incorrectas | Capturar número de empleado inexistente, inactivo o clave errónea. | Muestra mensaje genérico `"Número de empleado o clave incorrectos."` | Aprobado |
| CHE-04 | Restricción de domingo (RN) | Intentar registrar cualquier checada en día domingo. | Bloquea el registro con el mensaje `"No se checa los domingos: no hay horario."` | Aprobado |
| CHE-05 | Secuencia estricta de checadas | Intentar registrar `Salida` o `RegresoComida` sin haber registrado antes `Entrada` o `SalidaComida`. | Informa cuál es la siguiente checada esperada para el empleado en el día en curso. | Aprobado |

---

### 2.3. Catálogo de Puestos (`/Puestos`)

| ID | Caso de Prueba | Acción / Entrada | Resultado Esperado | Estado |
|----|----------------|------------------|--------------------|--------|
| PUE-01 | Consulta de catálogo | Abrir `/Puestos/Index`. | Lista todos los puestos registrados con su ID y nombre. | Aprobado |
| PUE-02 | Alta y edición de puesto | Crear puesto en `/Puestos/Nuevo` y editar nombre en `/Puestos/Editar/{id}`. | Persiste en MySQL y valida nombres vacíos, mayores a 50 caracteres o duplicados. | Aprobado |
| PUE-03 | Restricción de borrado (RN15) | Verificar acciones disponibles en `/Puestos/Index`. | No existe opción de eliminación (los puestos no se borran por regla de negocio y permisos SQL). | Aprobado |

---

### 2.4. Control de Ausencias (`/Ausencias`)

| ID | Caso de Prueba | Acción / Entrada | Resultado Esperado | Estado |
|----|----------------|------------------|--------------------|--------|
| AUS-01 | Consulta con filtros | Filtrar en `/Ausencias/Index` por empleado y rango de fechas (`Desde` / `Hasta`). | Muestra los registros filtrados con distintivo por tipo (`Justificada`, `Injustificada`, `Vacaciones`) y estado de semana. | Aprobado |
| AUS-02 | Registro de ausencia válida | Registrar ausencia en `/Ausencias/Nuevo` para un empleado activo en día hábil (lunes a sábado) sin checadas ese día. | Guarda correctamente y redirige al listado con alerta de éxito. | Aprobado |
| AUS-03 | Exclusión mutua checada vs. ausencia (RN17) | Intentar registrar una ausencia en una fecha donde el empleado ya tiene checadas (o ya tiene otra ausencia). | Rechaza la operación indicando el conflicto en esa fecha. | Aprobado |
| AUS-04 | Bloqueo en semana cerrada | Abrir `/Ausencias/Editar/{id}` o `/Ausencias/Eliminar/{id}` de una ausencia cuya semana ya está cerrada. | Deshabilita botones en el listado y muestra la vista en modo de **solo lectura**, impidiendo modificaciones o borrado. | Aprobado |

---

### 2.5. Asistencias y Reporte Diario (`/Asistencias`)

| ID | Caso de Prueba | Acción / Entrada | Resultado Esperado | Estado |
|----|----------------|------------------|--------------------|--------|
| ASI-01 | Captura manual de asistencia | Registrar en `/Asistencias/Nuevo` empleado, fecha, responsable (`Corregido por`) y horas en orden cronológico. | Guarda el registro con `Metodo = "Manual"` y registra el usuario responsable para auditoría automática en BD. | Aprobado |
| ASI-02 | Validación de orden y reglas de fecha | Intentar guardar horas desordenadas (`Salida < Entrada`), regreso sin salida a comer, fecha en domingo o fecha futura. | Muestra mensaje de validación específico y evita el guardado. | Aprobado |
| ASI-03 | Corrección y eliminación en semana abierta vs. cerrada | Intentar editar (`/Asistencias/Editar/{id}`) o eliminar (`/Asistencias/Eliminar/{id}`) un registro. | Permite modificar/eliminar si la semana está abierta; bloquea en modo **solo lectura** si `SemanaCerrada == true`. | Aprobado |
| ASI-04 | Reporte diario de asistencia | Consultar `/Asistencias/Reporte` por rango de fechas y empleado. | Despliega horas presentes, minutos de comida, comida extra, retardos, salidas tempranas y minutos extra desde `v_asistencia_diaria`. | Aprobado |

---

### 2.6. Pagos Semanales, Semanas y Tickets (`/Pagos`)

| ID | Caso de Prueba | Acción / Entrada | Resultado Esperado | Estado |
|----|----------------|------------------|--------------------|--------|
| PAG-01 | Gestión de semanas (`/Pagos/Semanas`) | Registrar nueva semana seleccionando un día lunes. | Calcula automáticamente el periodo de lunes a sábado (`FechaFin = FechaInicio + 5 días`) y rechaza días distintos de lunes o duplicados. | Aprobado |
| PAG-02 | Generación masiva de pagos | Seleccionar semana y usuario en `/Pagos/Index` y ejecutar `Generar pagos de la semana`. | Ejecuta `sp_generar_pago` para los empleados activos elegibles, reportando el conteo de generados y el detalle de omitidos. | Aprobado |
| PAG-03 | Alerta de días sin checada ni ausencia | Filtrar una semana con días hábiles sin registro en `/Pagos/Index` o `/Pagos/Ticket/{id}`. | Muestra advertencia con los días pendientes (`v_dias_sin_checada`) antes de permitir cerrar el pago. | Aprobado |
| PAG-04 | Consulta de ticket detallado | Hacer clic en `Ver Ticket` (`/Pagos/Ticket/{id}`). | Muestra sueldo base, valor por hora, descuentos, monto extra, salario a pagar y desglose por concepto (`sp_ticket`). | Aprobado |
| PAG-05 | Cierre definitivo y observaciones | Guardar observación y ejecutar `Cerrar Pago Definitivamente` en `/Pagos/Ticket/{id}`. | Cambia el estado a `Cerrado`, bloquea cambios futuros en checadas/ausencias/montos y mantiene habilitado el registro de observaciones. | Aprobado |

---

## 3. Pruebas de Interfaz y Responsividad

* **Menú de navegación superior (`_Layout.cshtml`):** Configurado con punto de quiebre `navbar-expand-xl` para colapsar en menú tipo sándwich antes de que las 8 opciones principales y submenús se superpongan en pantallas medianas o ventanas divididas.
* **Alineación de formularios y tablas:** Contenedores principales configurados con clase `.contenedor-ajustable` y filtros con botones proporcionales (`flex-fill`), evitando recortes horizontales en botones de acción.
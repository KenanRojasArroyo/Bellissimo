# 1. Planteamiento del problema

La Florería Bellissimo tiene 11 empleados (recepción, reparto y florería) con un horario único:
**lunes a viernes de 9:00 a 17:30 y sábado de 9:00 a 15:30**. Hoy la asistencia se anota a mano y
el pago semanal se calcula en papel. Esto provoca:

- **Errores de cálculo:** retardos, comida de más, salidas antes de tiempo, faltas y horas extra
  se suman a mano cada semana.
- **Pagos difíciles de justificar:** cuando un empleado pregunta por qué cobró menos, no hay un
  ticket con el detalle día por día.
- **Cambios sin rastro:** si alguien corrige una hora anotada, no queda registro de quién lo hizo
  ni del valor anterior.
- **Información repetida y contradictoria:** el sueldo, el puesto y el horario se copian en cada
  hoja semanal (ver anomalías en la sección de normalización).

**Objetivo de la base de datos.** Guardar las **cuatro checadas diarias** de cada empleado
(entrada, salida a comer, regreso y salida) y las ausencias, **calcular el pago semanal** con
descuentos y tiempo extra según las reglas acordadas con el dueño, **congelar** el pago ya
entregado con un ticket que lo justifique, y **auditar** toda corrección. La base la usan el
dueño y el encargado desde una página web (ASP.NET Core Razor Pages, a cargo del equipo de la
aplicación).

**Alcance.** Incluye empleados, puestos, usuarios de la página, horario, parámetros de cálculo,
checadas, ausencias, semanas, pagos, ticket y auditoría. **Fuera de alcance (etapa 2):**
bonificaciones y vacaciones por antigüedad. El checado por huella queda preparado
(`empleado.id_huella`, método `Huella`), pero todavía no se usa.

**Datos.** Todos los datos de prueba son **ficticios** (5 empleados, 2 semanas). No se usan
datos reales de los empleados.

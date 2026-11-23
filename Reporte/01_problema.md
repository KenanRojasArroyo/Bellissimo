# 1. Introducción y planteamiento del problema

## 1.1 Introducción

Este reporte documenta el diseño e implementación de la base de datos de un sistema de **reloj
checador y pago semanal** para la Florería Bellissimo, como proyecto final de la materia Base de
datos. El trabajo sigue el ciclo completo de una base de datos relacional: el análisis del
problema con el dueño del negocio, el modelo conceptual en cinco notaciones, la transformación a
tablas con su diccionario de datos, la normalización, el álgebra relacional y la implementación
en **MySQL 8.0**. Esta última incluye restricciones, vistas, funciones, procedimientos, triggers
y transacciones, además de las pruebas ejecutadas en el SGBD y las incidencias reales
encontradas.

La base de datos es la parte central del sistema: las reglas críticas del pago (descuentos,
tiempo extra, congelamiento del pago entregado y auditoría) se hacen cumplir **dentro del SGBD**,
para que ninguna pantalla pueda saltárselas. La aplicación web que la utiliza (ASP.NET Core Razor Pages .NET 10) está implementada en cuatro capas; su arquitectura, capturas y pruebas de interfaz se documentan en la sección 13. 
Para facilitar esa integración, la base entrega un contrato de uso (`Sql/CONTRATO_BD.md`) con
procedimientos, vistas, errores y ejemplos en C# probados contra la base.

## 1.2 Contexto

La Florería Bellissimo tiene 11 empleados (recepción, reparto y florería) con un horario único:
**lunes a viernes de 9:00 a 17:30 y sábado de 9:00 a 15:30**. El pago es semanal, con base en un
sueldo semanal pactado con cada empleado. El dueño y un encargado administran el personal.

## 1.3 Problema actual

Hoy la asistencia se anota a mano y el pago semanal se calcula en papel. Esto provoca:

- **Errores de cálculo:** retardos, comida de más, salidas antes de tiempo, faltas y horas extra
  se suman a mano cada semana.
- **Pagos difíciles de justificar:** cuando un empleado pregunta por qué cobró menos, no hay un
  ticket con el detalle día por día.
- **Cambios sin rastro:** si alguien corrige una hora anotada, no queda registro de quién lo hizo
  ni del valor anterior.
- **Información repetida y contradictoria:** el sueldo, el puesto y el horario se copian en cada
  hoja semanal (ver las anomalías en la sección 7).

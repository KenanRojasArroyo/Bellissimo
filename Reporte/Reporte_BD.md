# Proyecto final — Base de datos

**Sistema de reloj checador y pago semanal — Florería Bellissimo**

Universidad Autónoma de Baja California · Facultad de Ingeniería, Arquitectura y Diseño (FIAD)
· Ingeniería en Software y Tecnologías Emergentes (ISyTE) · Materia: Base de datos

**Equipo:** Bongles

| Integrante | Rol |
| ---------- | --- |
| Jose Antonio Montoya Garcia | Administrador de base de datos (DBA) |
| Troy Leonardo Moreno Calderon | Backend |
| Kenan Rojas Arroyo | Frontend |

**Maestro:** Julia Guadalupe Juarez Hernandez · **Fecha de entrega:** Domingo 22 de noviembre de 2026 · **Plataforma:** Classroom

**Repositorio:** https://github.com/KenanRojasArroyo/Bellissimo

---

## Contenido

1. Introducción y planteamiento del problema
2. Objetivos, alcance, usuarios, reglas de negocio y requerimientos
3. Arquitectura del sistema y tecnologías
4. Entidades, atributos, claves y relaciones
5. Diagramas y modelo relacional
6. Diccionario de datos
7. Anomalías y normalización
8. Álgebra relacional
9. Consultas SQL explicadas
10. Implementación en MySQL 8.0
11. Pruebas de base de datos
12. Incidencias
13. Aplicación web
14. Conclusiones
15. Referencias

<div class="salto"></div>

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

<div class="salto"></div>

# 2. Objetivos, alcance, usuarios, reglas de negocio y requerimientos

## 2.1 Objetivos

**Objetivo general.** Diseñar e implementar en MySQL 8.0 una base de datos relacional que
registre la asistencia diaria de los empleados de la Florería Bellissimo y calcule su pago
semanal según las reglas acordadas con el dueño. Cada pago debe quedar justificado con un ticket,
congelado una vez entregado y con rastro de toda corrección. La base la usará una aplicación web.

**Objetivos específicos.**

1. Modelar el negocio (empleados, puestos, horario, checadas, ausencias, semanas, pagos, ticket y
   auditoría) en un modelo entidad–relación documentado en cinco notaciones.
2. Obtener un esquema relacional en 3FN, con desnormalización controlada y justificada, y su
   diccionario de datos generado del SGBD.
3. Hacer cumplir en la base las reglas críticas con restricciones (PK, FK, UNIQUE, NOT NULL,
   CHECK), triggers y procedimientos almacenados con transacciones.
4. Calcular el pago semanal (retardos, comida de más, salidas tempranas, faltas y tiempo extra)
   con parámetros configurables y guardar el ticket que lo justifica.
5. Proteger la información con un usuario de mínimo privilegio, auditoría de correcciones y un
   procedimiento de respaldo y restauración probado.
6. Comprobar el funcionamiento con casos de prueba ejecutados en el SGBD (válidos e inválidos) y
   documentar las incidencias reales.
7. Entregar al equipo de la aplicación un contrato de uso de la base para que la interfaz no
   duplique ni contradiga la lógica del SGBD.

## 2.2 Alcance

**Incluye:** empleados y puestos, usuarios de la página (dueño y encargado), horario único,
parámetros del cálculo, las cuatro checadas diarias, ausencias (justificada, injustificada y
vacaciones), semanas de pago, pago semanal con su ticket, cierre (congelamiento) del pago y
auditoría de correcciones.

**Fuera de alcance (etapa 2):** bonificaciones y vacaciones por antigüedad. El checado por huella
queda preparado (`empleado.id_huella`, método `Huella`), pero todavía no se usa.

**Estado de la aplicación:** la interfaz web fue desarrollada por el equipo de la aplicación, las pantallas, la autenticación y su despliegue.

**Datos:** todos los datos de prueba son **ficticios** (5 empleados, 2 semanas). No se usan datos
reales de los empleados.

## 2.3 Actores y usuarios

| Actor | Tipo | Cómo interactúa con el sistema | Cuenta / acceso |
| ----- | ---- | ------------------------------ | --------------- |
| **Dueño** | Usuario de la página | Configura parámetros del cálculo y el horario; consulta pagos, reportes y auditoría; corrige checadas olvidadas | `usuario` con rol `Dueno` |
| **Encargado** | Usuario de la página | Registra ausencias, corrige checadas (queda auditado), genera, revisa y cierra pagos, agrega observaciones, consulta el ticket | `usuario` con rol `Encargado` |
| **Empleado** | Actor del checador | Registra sus 4 checadas diarias con su clave personal (huella en el futuro); no inicia sesión en la página | Fila en `empleado` (`clave_hash`) |
| **Aplicación web** | Sistema | Ejecuta procedimientos y consultas en nombre de los usuarios | Cuenta MySQL `app_floreria` (mínimo privilegio) |
| **DBA** | Administrador | Crea y mantiene la base, respaldos y pruebas | Cuenta administrativa de MySQL (Workbench) |
| **Contador** | Interesado externo | No usa el sistema; debe confirmar los multiplicadores de tiempo extra | — |

## 2.4 Datos que se registran

| Grupo | Datos | Tabla(s) |
| ----- | ----- | -------- |
| Personal | Nombre y apellidos, teléfono, fecha de ingreso, puesto, sueldo base semanal, clave personal (hash), identificador de huella, si está activo | `empleado`, `puesto` |
| Usuarios de la página | Nombre, usuario, contraseña (hash), rol, si está activo | `usuario` |
| Reglas configurables | Horario por día; horas base (48), tolerancia (5 min), comida (30 min), umbral y multiplicadores de extra, umbrales de retardo (pendientes) | `horario_general`, `configuracion` |
| Asistencia | Por empleado y día: entrada, salida a comer, regreso, salida, método (Clave, Huella o Manual) y quién corrigió | `registro_asistencia` |
| Ausencias | Fecha, tipo (justificada, injustificada o vacaciones) y observaciones | `ausencia` |
| Pago | Semana; por empleado: sueldo y valor de la hora usados, minutos por concepto, descuentos, extra, salario final, estado (borrador o cerrado), observaciones | `semana`, `pago_semanal` |
| Ticket | Un renglón por concepto y día: minutos y monto | `detalle_pago` |
| Auditoría | Antes y después de cada corrección, usuario de la página, cuenta de MySQL y fecha | `auditoria_registro` |

## 2.5 Decisiones que requieren información

| Decisión | Quién la toma | Información que necesita | De dónde sale |
| -------- | ------------- | ------------------------ | ------------- |
| Cuánto pagarle a cada empleado esta semana | Encargado / dueño | Checadas, ausencias, parámetros y el cálculo con su detalle | `sp_generar_pago`, `sp_ticket`, `v_resumen_pago_semanal` |
| Si ya se puede cerrar la semana o falta capturar algo | Encargado | Días laborales sin checada ni ausencia | `v_dias_sin_checada` |
| Cómo responder a un empleado que reclama su pago | Encargado / dueño | Ticket congelado, renglón por renglón | `sp_ticket` (datos guardados, no recalculados) |
| Si una corrección de horario fue legítima | Dueño | Valor anterior y nuevo, quién y cuándo | `auditoria_registro` |
| A quién hablarle por puntualidad | Dueño | Retardos y comida de más acumulados; ranking de puntualidad | Consultas 6 y 9; `v_asistencia_diaria` |
| Cuánto cuesta la nómina semanal y el tiempo extra | Dueño | Totales, promedio, descuentos y extra por semana | Consulta 7 |
| Qué puestos generan más descuentos | Dueño | Incidencias y montos por puesto | Consulta 8 |
| Si conviene cambiar una regla (tolerancia, multiplicador, retardos) | Dueño (con el contador) | Parámetros vigentes y su efecto en los pagos | `configuracion`; regenerar pagos en borrador |

## 2.6 Operaciones CRUD previstas

C = crear · R = consultar · U = modificar · D = eliminar. Lo marcado *(proc.)* se hace solo con un
procedimiento almacenado y lo marcado *(trigger)* lo escribe la base sola. Los permisos coinciden
con `Sql/05_usuario_app.sql`.

| Entidad | C | R | U | D | Notas |
| ------- | - | - | - | - | ----- |
| `empleado` | Sí | Sí | Sí | Sí, sin historial | Baja normal: `activo = 0` (las FK impiden borrar a quien tiene historial) |
| `puesto` | Sí | Sí | Sí | No | Catálogo |
| `usuario` | Sí | Sí | Sí | No | Baja con `activo = 0` |
| `horario_general`, `configuracion` | No | Sí | Sí | No | Solo se ajustan valores |
| `registro_asistencia` | Sí *(proc. `sp_registrar_checada`)* | Sí | Sí, corrección | Sí, corrección | Las correcciones quedan auditadas; bloqueado si la semana está cerrada |
| `ausencia` | Sí | Sí | Sí | Sí | Bloqueada si la semana está cerrada o si ese día hay checadas |
| `semana` | Sí | Sí | No | No | Una por lunes |
| `pago_semanal` | Sí *(proc. `sp_generar_pago`)* | Sí | *(proc.)* regenerar, cerrar, observación | No | Cerrado = congelado |
| `detalle_pago` | *(proc.)* | Sí | No | *(proc.)* al regenerar | Solo lo escribe `sp_generar_pago` |
| `auditoria_registro` | *(trigger)* | Sí | No | No | La app no escribe aquí |

**Operación de negocio que afecta varias tablas:** `sp_generar_pago` lee `empleado`,
`semana`, `configuracion`, `horario_general`, `registro_asistencia` y `ausencia`, y escribe
`pago_semanal` y `detalle_pago` dentro de una sola transacción.

## 2.7 Reglas de negocio (verificables)

Reglas acordadas con el dueño de la florería. Cada una dice **qué mecanismo de la base la hace
cumplir** y **con qué prueba se verifica** (`Pruebas/pruebas_bd.sql`; resultados en la
sección 11). Las marcadas *Supuesto* o *Pendiente* no las cerró el DBA.

| ID | Regla | Mecanismo en la BD | Verificación |
| -- | ----- | ------------------ | ------------ |
| RN01 | Horario único: lunes a viernes 9:00–17:30, sábado 9:00–15:30; domingo no se trabaja. | Datos de `horario_general`; `ck_horario_dia` (1–6), `ck_horario_horas` (salida > entrada); `sp_registrar_checada` rechaza domingos. | P07; consulta a `horario_general` |
| RN02 | El sueldo base semanal se captura al dar de alta y debe ser mayor a 0. | `empleado.sueldo_base_semanal NOT NULL` + `ck_empleado_sueldo`. | P01, P04 |
| RN03 | Valor de la hora = sueldo base ÷ 48. | `fn_valor_hora` + `configuracion.horas_semana_base = 48`. | P09 (`fn_valor_hora(3300) = 68.7500`) |
| RN04 | Un solo registro de asistencia por empleado y día, con máximo 4 checadas en orden (entrada, salida a comer, regreso, salida). | `uq_registro_dia`; `ck_registro_orden`; `ck_registro_fecha`; `sp_registrar_checada` decide la siguiente y rechaza una quinta o una hora anterior. | P07, P08, P15b, P15c, P15d |
| RN05 | Tolerancia de 5 min; pasando la tolerancia el retardo se mide desde la hora oficial. | `configuracion.tolerancia_min = 5`; `sp_generar_pago` (renglón `RETARDO`); `v_asistencia_diaria`. | P10, P13 |
| RN06 | Comida de 30 min pagada; el exceso se descuenta. | `configuracion.comida_min = 30`; `fn_minutos_comida_extra`; `sp_generar_pago` (`COMIDA_EXCESO`). | P09, regresión (María semana 1: 85 min → $2,911.45) |
| RN07 | La salida antes de la hora oficial se descuenta. | `sp_generar_pago` (`SALIDA_TEMPRANA`). | P10 (Juan: 34.38) |
| RN08 | Falta injustificada descuenta la jornada del día (510 min entre semana, 390 el sábado); justificada (solo salud) y vacaciones no se descuentan. *La justificada pagada es un **supuesto**.* | `ausencia.tipo ENUM('Justificada','Injustificada','Vacaciones')`; `uq_ausencia_dia`; `sp_generar_pago` (`FALTA_INJUSTIFICADA`). | P10 (Juan: 584.38); regresión (Luis semana 1 = $3,150.00 con vacaciones y falta justificada) |
| RN09 | Retardos acumulados en la semana: regla **pendiente**; mientras no se defina, no descuentan. | `configuracion.retardo_min_para_1h/2h = NULL`; `sp_generar_pago` solo descuenta si tienen valor. | Regresión (Juan semana 1 sin `RETARDO_ACUMULADO`) |
| RN10 | Tiempo extra: los primeros 540 min de la semana ×2 y el resto ×3. *Multiplicadores por **confirmar** con el contador.* | `configuracion.extra_umbral_min/mult_doble/mult_triple`; `sp_generar_pago` (`TIEMPO_EXTRA`, con función de ventana). | Regresión (Juan semana 2: 720 min → $1,856.25) |
| RN11 | Salario = sueldo − descuentos + extra, nunca menor a $0. | `GREATEST(0, …)` en `sp_generar_pago`; `ck_pago_montos`. | P10, regresión |
| RN12 | Un solo pago por empleado y semana. | `uq_pago_empleado_semana`; `sp_generar_pago` actualiza si ya existe. | P10 (regenerar no duplica) |
| RN13 | Un pago cerrado queda congelado: ni checadas, ni ausencias, ni ticket, ni montos de esa semana; solo observaciones. | `sp_cerrar_pago`; `fn_semana_cerrada`; triggers `trg_registro_bi/bu/bd_cerrado`, `trg_ausencia_bi/bu/bd_cerrado`, `trg_detalle_bi/bu/bd_cerrado`, `trg_pago_bu_cerrado`, `trg_pago_bd_cerrado`; `sp_generar_pago` rechaza regenerar. | P14, P14a–e, P17, P17a–b, P18, P18a–b |
| RN14 | Toda corrección manual de una checada queda registrada (antes, después, usuario de la app y usuario de BD). | `trg_registro_au_auditoria` → `auditoria_registro`. | P13 |
| RN15 | No se borra información con historial (empleados, puestos, semanas, pagos con ticket). | FK con `ON DELETE RESTRICT`; baja lógica con `activo = 0`. | P05, P06 |
| RN16 | Contraseñas y claves personales se guardan solo como hash. | Convención: `usuario.contrasena` y `empleado.clave_hash` `VARCHAR(255)`; **lo hace la app** (la BD no puede verificar que sea hash). | Revisión de la app (Sección 13.5) |
| RN17 | Un día es **checada o ausencia**, nunca las dos, para el mismo empleado (decisión del 2026-11-16, PEN-01). | `trg_registro_bi_ausencia`, `trg_registro_bu_ausencia`, `trg_ausencia_bi_checada`, `trg_ausencia_bu_checada`. | P21, P21a–d |

Decisiones pendientes con el dueño (documentadas, no cerradas): regla de retardos acumulados
(RN09), si la falta justificada se paga (RN08), multiplicadores de tiempo extra (RN10), quién
corrige checadas olvidadas. El hallazgo PEN-01 (checada y ausencia el mismo día) ya se resolvió con
la regla RN17.

## 2.8 Requerimientos funcionales

| ID | Requerimiento | Lo cumple | Verificación |
| -- | ------------- | --------- | ------------ |
| RF01 | Registrar las 4 checadas del día con fecha y hora exactas, por clave (y huella en el futuro). | `sp_registrar_checada` | P07, P08 |
| RF02 | Dar de alta, consultar, modificar y dar de baja empleados. | Tabla `empleado` + `ck_empleado_sueldo` + FK; baja con `activo = 0` | P01, P02, P03, P04, P05, P06 |
| RF03 | Registrar ausencias (justificada, injustificada, vacaciones). | Tabla `ausencia` + `uq_ausencia_dia` + `trg_ausencia_*` | P18 |
| RF04 | Corregir una checada a mano con rastro de quién y qué cambió. | `UPDATE registro_asistencia` (con `modificado_por`) + `trg_registro_au_auditoria` | P13 |
| RF05 | Calcular el pago semanal con descuentos y tiempo extra, sin dejar datos a medias si algo falla. | `sp_generar_pago` (`START TRANSACTION … COMMIT`, `ROLLBACK` en error) | P10 |
| RF06 | Mostrar el ticket que justifica el pago sin recalcular. | `sp_ticket` (2 resultados: resumen y detalle) | P10 |
| RF07 | Cerrar (congelar) un pago ya pagado. | `sp_cerrar_pago` + triggers de semana cerrada | P14, P17 |
| RF08 | Agregar observaciones a un pago, incluso cerrado. | `sp_agregar_observacion` | P14 |
| RF09 | Reporte de asistencia diaria por empleado. | `v_asistencia_diaria` | Consultas 6 y vista en `04_consultas_vistas.sql` |
| RF10 | Resumen de pagos de la semana con estado. | `v_resumen_pago_semanal` | P16 |
| RF11 | Avisar de días laborales sin checada ni ausencia antes de cerrar la semana. | `v_dias_sin_checada` | P19 |
| RF12 | Cambiar parámetros del cálculo sin modificar el sistema. | Tabla `configuracion` (leída por funciones y procedimientos) | Revisión de `sp_generar_pago` |
| RF13 | Consultar el historial de correcciones de checadas. | Tabla `auditoria_registro` | P13 |
| RF14 | Iniciar sesión con rol Dueño o Encargado. | Tabla `usuario` (`rol`, `activo`, hash); la validación del hash la hace la app | Pendiente — equipo de la aplicación |

<div class="salto"></div>

# 3. Arquitectura del sistema y tecnologías

## 3.1 Esquema de arquitectura

![Arquitectura del sistema](../Diagramas/arquitectura.png)

Archivo editable: `Diagramas/arquitectura.svg` (se abre en Inkscape, diagrams.net o cualquier
editor de texto). Las cajas punteadas dependen del equipo de la aplicación.

Flujo: el **usuario** usa la **interfaz web**; la aplicación se conecta al **SGBD** (MySQL) con
una cuenta de mínimo privilegio y ejecuta procedimientos y consultas; el SGBD guarda y protege los
datos en la **base de datos** `belissimo`. Las reglas críticas viven en el SGBD, así que se
cumplen sin importar qué pantalla haga la operación.

## 3.2 Tecnologías

| Capa | Tecnología | Estado |
| ---- | ---------- | ------ |
| Usuarios | Navegador web en la computadora de la florería | Listo |
| Interfaz / aplicación | ASP.NET Core Razor Pages (.NET), en capas Web, Negocios, Datos y Entidades (estructura actual del repositorio); acceso a datos con ADO.NET y el conector MySql.Data | Listo |
| SGBD | MySQL Community Server 8.0.46, motor InnoDB, `utf8mb4` / `utf8mb4_unicode_ci` | Implementado y probado |
| Base de datos | `belissimo`: 11 tablas, 3 vistas, 3 funciones, 5 procedimientos, 16 triggers | Implementado y probado |
| Herramientas | MySQL Workbench 8.0 (administración y diagramas), cliente `mysql`, `mysqldump`, Git y GitHub | En uso |

## 3.3 Mecanismos de acceso

- La aplicación se conecta por **TCP al puerto 3306** del servidor MySQL (en la misma computadora:
  `localhost`) con la cuenta **`app_floreria`** (`Sql/05_usuario_app.sql`).
- Usa **procedimientos almacenados** para las operaciones de negocio (`sp_registrar_checada`,
  `sp_generar_pago`, `sp_ticket`, `sp_cerrar_pago`, `sp_agregar_observacion`), **vistas** para los
  reportes y `INSERT`/`UPDATE`/`DELETE` con parámetros solo en las tablas del CRUD. La referencia
  completa está en `Sql/CONTRATO_BD.md`, cuyos ejemplos en C# se ejecutaron contra la base.
- La cadena de conexión se guarda en **user-secrets** (desarrollo) o en una **variable de entorno**
  (equipo de la florería), nunca en el código ni en el navegador (`Sql/conexion_plantilla.md`).
- El DBA administra la base con una cuenta administrativa desde MySQL Workbench.

## 3.4 Respaldo

- `Sql/respaldo_restauracion.ps1` genera respaldos con `mysqldump --single-transaction --routines
  --triggers --events`; así incluye datos, procedimientos, funciones, triggers y vistas.
- Los respaldos se guardan en `Sql/respaldos/`, que no se sube a GitHub porque puede contener
  datos personales. Se recomienda una copia fuera de la computadora.
- La restauración se **probó** en una base aparte: las 11 tablas dieron el mismo `CHECKSUM`
  (secciones 10.7 y 11).

## 3.5 Seguridad

- **Mínimo privilegio:** `app_floreria` no puede usar `DROP`, `ALTER` ni `CREATE`, no tiene
  acceso a otras bases y solo lee pagos y auditoría (17/17 casos de permisos verificados).
- **Credenciales fuera del repositorio** y contraseñas y claves personales guardadas solo como hash.
- **Integridad en el SGBD:** restricciones, triggers y procedimientos impiden datos inválidos,
  aunque la interfaz falle (sección 10.2).
- **Auditoría:** toda corrección de una checada guarda el antes, el después, el usuario de la
  página y la cuenta de MySQL.

<div class="salto"></div>

# 4. Entidades, atributos, claves y relaciones

Base `belissimo` (MySQL 8.0). Todo lo que se dice de restricciones y llaves se puede comprobar en
la sección 6 (diccionario de datos), que se generó desde `information_schema`.

## 4.1 Entidades

| Entidad (tabla) | Clasificación | Justificación (regla de negocio) |
| --------------- | ------------- | -------------------------------- |
| `puesto` | **Fuerte** | Existe por sí misma; es un catálogo (Recepcionista, Repartidor, Florista). |
| `empleado` | **Fuerte** | Se identifica por sí mismo (`id_empleado`). Depende de `puesto` solo por referencia: todo empleado tiene un puesto, pero su identidad no viene del puesto. |
| `usuario` | **Fuerte** | Dueño y encargado de la página; identidad propia (`id_usuario`, `nombre_usuario`). |
| `semana` | **Fuerte** | Periodo de pago lunes–sábado identificado por su fecha de inicio. |
| `horario_general` | **Fuerte** (catálogo) | Un renglón por día de la semana (1–6); el horario es único para todos. |
| `configuracion` | **Fuerte** (catálogo) | Parámetros que el dueño puede cambiar sin tocar el sistema. |
| `registro_asistencia` | **Débil** (por existencia y por identificación) | Una checada no tiene sentido sin el empleado. Su identificador natural es (`id_empleado`, `fecha`), que incluye la llave del dueño. Se implementa con PK sustituta `id_registro` más `UNIQUE (id_empleado, fecha)`. |
| `ausencia` | **Débil** | Igual que la checada: existe solo para un empleado, y su identificador natural es (`id_empleado`, `fecha`). |
| `pago_semanal` | **Asociativa** | Resuelve la relación muchos-a-muchos **empleado–semana** (un empleado cobra muchas semanas; en una semana cobran muchos empleados) y tiene atributos propios (montos, minutos, estado). Su llave natural es (`id_empleado`, `id_semana`). |
| `detalle_pago` | **Débil** (por existencia) | Un renglón del ticket solo existe dentro de un pago. No tiene llave natural declarada (ver §4.3). |
| `auditoria_registro` | **Fuerte** (bitácora) | A propósito **no** tiene FK: debe conservarse aunque el registro auditado se borre. |

> **Nota de implementación.** Todas las tablas débiles usan una PK sustituta `AUTO_INCREMENT`, y su
> llave natural queda como `UNIQUE`. Por eso en el modelo físico todas las relaciones son **no
> identificadoras**: la FK no forma parte de la PK. Así aparece en el diagrama IDEF1X, con líneas
> punteadas. Se eligió así porque la app y las FK trabajan con un solo entero, y porque la llave
> natural sigue protegida.

## 4.2 Clasificación de atributos

| Clase | Qué significa | Atributos del modelo |
| ----- | ------------- | -------------------- |
| **Identificadores** | Distinguen cada ocurrencia | `id_puesto`, `id_empleado`, `id_usuario`, `dia_semana`, `parametro`, `id_registro`, `id_ausencia`, `id_semana`, `id_pago`, `id_detalle`, `id_auditoria` (PK); llaves naturales en §4.3 |
| **Simples** (atómicos) | No se dividen en partes con significado propio | Casi todos: `telefono`, `fecha_ingreso`, `sueldo_base_semanal`, `rol`, `metodo`, `tipo`, `concepto`, `minutos`, `monto`, `hora_entrada`, `valor`, etc. |
| **Compuestos** | En el negocio se forman con varias partes | *Nombre del empleado* = `nombre` + `apellido_paterno` + `apellido_materno` (se guarda descompuesto para ordenar y buscar por apellido). *Periodo de pago* = `fecha_inicio` + `fecha_fin`. *Momento de una checada* = fecha + hora: se guarda como un `DATETIME` (atómico para SQL) y la fecha se repite en `registro_asistencia.fecha` porque forma parte de la llave natural. `usuario.nombre_completo` se deja como simple a propósito: solo se muestra, no se busca por partes. |
| **Multivaluados** (en el modelo conceptual) | Un empleado puede tener varios valores | Las checadas de un empleado → `registro_asistencia`; sus ausencias → `ausencia`; los renglones de un pago → `detalle_pago`. Cada uno se convirtió en tabla (sección 7). Las **4 checadas del día no son multivaluadas**: son 4 roles distintos y fijos (entrada, salida a comer, regreso, salida), con orden obligatorio (`ck_registro_orden`), así que van en 4 columnas. Teléfono: *supuesto*, uno por empleado. |
| **Derivados guardados** (foto histórica, sección 7.6) | Se pueden calcular, pero se guardan a propósito | `pago_semanal`: `valor_hora` (= sueldo ÷ 48), `min_retardo`, `min_salida_temprana`, `min_falta`, `min_exceso_comida`, `min_extra`, `descuento_total`, `monto_extra` y `salario_final`; `detalle_pago.monto` (minutos × valor de la hora); `semana.fecha_fin` (= inicio + 5 días); `registro_asistencia.fecha` (= `DATE(entrada)`, garantizado por `ck_registro_fecha`); `auditoria_registro.id_empleado` y `fecha` (copias). |
| **Derivados calculados** (no se guardan) | Se obtienen al consultar | Día de la semana (`WEEKDAY(fecha) + 1`); horas presentes, minutos de retardo, comida de más y extra por día (`v_asistencia_diaria`, `fn_minutos_comida_extra`); días sin checada (`v_dias_sin_checada`). |
| **Obligatorios** (`NOT NULL`) | Deben tener valor siempre | Todos los atributos **excepto** los de la fila siguiente. |
| **Opcionales** (admiten `NULL`) | Pueden faltar, con motivo | `empleado`: `apellido_materno`, `telefono`, `clave_hash` (empleado aún sin clave), `id_huella` (huella futura). `registro_asistencia`: `entrada`, `salida_comida`, `regreso_comida` y `salida` (se llenan durante el día), `modificado_por` (solo en correcciones). `ausencia.observaciones`. `configuracion`: `valor` (NULL = regla pendiente) y `descripcion`. `pago_semanal`: `id_usuario` y `observaciones`. `detalle_pago.fecha` (NULL en conceptos semanales). `auditoria_registro`: las 8 horas anteriores y nuevas, e `id_usuario_app`. |

## 4.3 Claves candidatas

| Tabla | PK elegida | Otras claves candidatas | Cómo se garantiza |
| ----- | ---------- | ----------------------- | ----------------- |
| `puesto` | `id_puesto` | `nombre` | `UNIQUE` |
| `empleado` | `id_empleado` | `id_huella` *(solo cuando no es NULL)* | `UNIQUE` (MySQL permite varios NULL) |
| `usuario` | `id_usuario` | `nombre_usuario` | `UNIQUE` |
| `horario_general` | `dia_semana` | — | PK + `ck_horario_dia` (1–6) |
| `configuracion` | `parametro` | — | PK natural |
| `registro_asistencia` | `id_registro` | (`id_empleado`, `fecha`) | `uq_registro_dia` |
| `ausencia` | `id_ausencia` | (`id_empleado`, `fecha`) | `uq_ausencia_dia` |
| `semana` | `id_semana` | `fecha_inicio`; también `fecha_fin` (semánticamente) | `UNIQUE (fecha_inicio)`; `fecha_fin` no está declarada como única |
| `pago_semanal` | `id_pago` | (`id_empleado`, `id_semana`) | `uq_pago_empleado_semana` |
| `detalle_pago` | `id_detalle` | En la práctica (`id_pago`, `fecha`, `concepto`), porque el procedimiento genera un renglón por concepto y día, pero **no está declarada** | Solo `sp_generar_pago` escribe aquí |
| `auditoria_registro` | `id_auditoria` | — | Bitácora: puede haber varios cambios del mismo registro |

## 4.4 Relaciones: cardinalidad y participación

Lectura: "padre (cardinalidad) — hijo (cardinalidad)". La participación del **hijo** es **total**
cuando la FK es `NOT NULL`.

| # | Relación (FK) | Cardinalidad | Participación del hijo | Participación del padre | Regla de negocio que la justifica |
| - | ------------- | ------------ | ---------------------- | ----------------------- | --------------------------------- |
| R1 | `puesto` – `empleado` (`fk_empleado_puesto`) | 1 : 0..N | **Total** (`id_puesto NOT NULL`) | Parcial (un puesto puede no tener empleados) | Todo empleado se da de alta con un puesto. |
| R2 | `empleado` – `registro_asistencia` (`fk_registro_empleado`) | 1 : 0..N | **Total** | Parcial (Carla no tenía checadas) | Cada checada es de un empleado; un empleado nuevo aún no checa. |
| R3 | `usuario` – `registro_asistencia` (`fk_registro_usuario`, `modificado_por`) | 0..1 : 0..N | **Parcial** (NULL si nadie la corrigió) | Parcial | Solo las correcciones a mano llevan quién las hizo. |
| R4 | `empleado` – `ausencia` (`fk_ausencia_empleado`) | 1 : 0..N | **Total** | Parcial | Una ausencia siempre es de un empleado. |
| R5 | `empleado` – `pago_semanal` (`fk_pago_empleado`) | 1 : 0..N | **Total** | Parcial | Cada pago es de un empleado; uno nuevo aún no tiene pagos. |
| R6 | `semana` – `pago_semanal` (`fk_pago_semana`) | 1 : 0..N | **Total** | Parcial | Cada pago corresponde a una semana. |
| R7 | `usuario` – `pago_semanal` (`fk_pago_usuario`) | 0..1 : 0..N | **Parcial** (`id_usuario` admite NULL) | Parcial | Se guarda qué encargado generó el pago, si se conoce. |
| R8 | `pago_semanal` – `detalle_pago` (`fk_detalle_pago`) | 1 : 0..N | **Total** | Parcial: una **semana perfecta no tiene renglones** (Ana, semana 1) | El ticket se arma con los renglones del pago. |

Relación muchos-a-muchos resuelta: **empleado N : M semana**, mediante `pago_semanal`, con a lo más
un pago por pareja (`uq_pago_empleado_semana`).

Todas las FK usan `ON UPDATE CASCADE` y `ON DELETE RESTRICT`. No se puede borrar un empleado,
puesto, semana o pago con historial; se da de baja con `activo = 0`.

<div class="salto"></div>

# 5. Diagramas y modelo relacional

Los cinco diagramas muestran **el mismo modelo** (11 tablas, 8 llaves foráneas), tomado de la
base real en MySQL 8.0.46. La explicación de cada notación y de cuál conviene está en
`Diagramas/README.md`. En resumen: se recomienda **Crow's Foot**, porque muestra en un solo
símbolo la cardinalidad y la participación (obligatoria u opcional).

## 5.1 Crow's Foot

![Crow's Foot](../Diagramas/crows_foot.png)

## 5.2 Classic

![Classic](../Diagramas/classic.png)

## 5.3 Connect to Columns

![Connect to Columns](../Diagramas/connect_to_columns.png)

## 5.4 UML

![UML](../Diagramas/uml.png)

## 5.5 IDEF1X

![IDEF1X](../Diagramas/idef1x.png)

## 5.6 Modelo relacional

Notación: **subrayado** = llave primaria · *cursiva* = llave foránea (→ tabla referenciada) ·
`UQ` = llave única.

- **puesto**(<u>id_puesto</u>, nombre `UQ`)
- **empleado**(<u>id_empleado</u>, *id_puesto* → puesto, nombre, apellido_paterno,
  apellido_materno, telefono, fecha_ingreso, sueldo_base_semanal, clave_hash, id_huella `UQ`,
  activo)
- **usuario**(<u>id_usuario</u>, nombre_completo, nombre_usuario `UQ`, contrasena, rol, activo)
- **horario_general**(<u>dia_semana</u>, hora_entrada, hora_salida)
- **configuracion**(<u>parametro</u>, valor, descripcion)
- **registro_asistencia**(<u>id_registro</u>, *id_empleado* → empleado, fecha, entrada,
  salida_comida, regreso_comida, salida, metodo, *modificado_por* → usuario)
  · `UQ(id_empleado, fecha)`
- **ausencia**(<u>id_ausencia</u>, *id_empleado* → empleado, fecha, tipo, observaciones)
  · `UQ(id_empleado, fecha)`
- **semana**(<u>id_semana</u>, fecha_inicio `UQ`, fecha_fin)
- **pago_semanal**(<u>id_pago</u>, *id_empleado* → empleado, *id_semana* → semana,
  *id_usuario* → usuario, sueldo_base_usado, valor_hora, min_retardo, min_salida_temprana,
  min_falta, min_exceso_comida, min_extra, descuento_total, monto_extra, salario_final, cerrado,
  fecha_generacion, observaciones) · `UQ(id_empleado, id_semana)`
- **detalle_pago**(<u>id_detalle</u>, *id_pago* → pago_semanal, fecha, concepto, minutos, monto)
- **auditoria_registro**(<u>id_auditoria</u>, id_registro, id_empleado, fecha,
  entrada_anterior, salida_comida_anterior, regreso_comida_anterior, salida_anterior,
  entrada_nueva, salida_comida_nueva, regreso_comida_nueva, salida_nueva, id_usuario_app,
  usuario_bd, fecha_cambio) — sin FK, a propósito

Todas las FK: `ON UPDATE CASCADE`, `ON DELETE RESTRICT`. Tipos, longitudes, nulos y valores por
omisión de cada columna: sección 6, diccionario de datos.

<div class="salto"></div>

# 6. Diccionario de datos — base `belissimo`

Generado automáticamente desde `information_schema` de **MySQL 8.0.46** con `Sql/diccionario_datos.sql` (no está escrito a mano). El significado de cada atributo es el `COMMENT` definido en `Sql/01_esquema.sql`.

Tablas: **11** · Columnas: **80** · CHECK: **7** · Llaves foráneas: **8** · Índices: **27** · Triggers: **16**

## 6.1 Tablas

| tabla | descripcion | motor | intercalacion |
| --- | --- | --- | --- |
| auditoria_registro | Bitácora de correcciones a checadas (la llena un trigger) | InnoDB | utf8mb4_unicode_ci |
| ausencia | Días que un empleado no asistió y su tipo | InnoDB | utf8mb4_unicode_ci |
| configuracion | Parámetros del cálculo de pago editables sin cambiar el sistema | InnoDB | utf8mb4_unicode_ci |
| detalle_pago | Renglones del ticket de pago tal como se calcularon | InnoDB | utf8mb4_unicode_ci |
| empleado | Personas que checan asistencia y reciben pago semanal | InnoDB | utf8mb4_unicode_ci |
| horario_general | Horario oficial por día de la semana (lunes a sábado) | InnoDB | utf8mb4_unicode_ci |
| pago_semanal | Resumen congelado del pago de un empleado en una semana | InnoDB | utf8mb4_unicode_ci |
| puesto | Catálogo de puestos de trabajo | InnoDB | utf8mb4_unicode_ci |
| registro_asistencia | Las cuatro checadas de un empleado en un día | InnoDB | utf8mb4_unicode_ci |
| semana | Periodo de pago de lunes a sábado | InnoDB | utf8mb4_unicode_ci |
| usuario | Personas que usan la página web (dueño y encargado) | InnoDB | utf8mb4_unicode_ci |

## 6.2 Atributos por tabla

### `auditoria_registro`

_Bitácora de correcciones a checadas (la llena un trigger)_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_auditoria | Identificador del cambio auditado | int | 10 | No | PK |  |  | auto_increment |
| id_registro | Registro corregido (sin FK: sobrevive a borrados) | int | 10 | No |  |  |  |  |
| id_empleado | Empleado del registro corregido | int | 10 | No |  |  |  |  |
| fecha | Día del registro corregido | date |  | No |  |  |  |  |
| entrada_anterior | Entrada antes del cambio | datetime |  | Sí |  |  |  |  |
| salida_comida_anterior | Salida a comer antes del cambio | datetime |  | Sí |  |  |  |  |
| regreso_comida_anterior | Regreso de comer antes del cambio | datetime |  | Sí |  |  |  |  |
| salida_anterior | Salida antes del cambio | datetime |  | Sí |  |  |  |  |
| entrada_nueva | Entrada después del cambio | datetime |  | Sí |  |  |  |  |
| salida_comida_nueva | Salida a comer después del cambio | datetime |  | Sí |  |  |  |  |
| regreso_comida_nueva | Regreso de comer después del cambio | datetime |  | Sí |  |  |  |  |
| salida_nueva | Salida después del cambio | datetime |  | Sí |  |  |  |  |
| id_usuario_app | Usuario de la página que corrigió (modificado_por) | int | 10 | Sí |  |  |  |  |
| usuario_bd | Usuario de MySQL que ejecutó el cambio | varchar(100) | 100 | No |  |  |  |  |
| fecha_cambio | Momento del cambio | datetime |  | No |  |  | CURRENT_TIMESTAMP | DEFAULT_GENERATED |

### `ausencia`

_Días que un empleado no asistió y su tipo_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_ausencia | Identificador de la ausencia | int | 10 | No | PK |  |  | auto_increment |
| id_empleado | Empleado que faltó | int | 10 | No |  | empleado.id_empleado (fk_ausencia_empleado) |  | uq_ausencia_dia |
| fecha | Día de la ausencia | date |  | No |  |  |  | uq_ausencia_dia |
| tipo | Justificada (salud), Injustificada o Vacaciones | enum('Justificada','Injustificada','Vacaciones') | 13 | No |  |  |  |  |
| observaciones | Motivo o nota | varchar(200) | 200 | Sí |  |  |  |  |

### `configuracion`

_Parámetros del cálculo de pago editables sin cambiar el sistema_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| parametro | Nombre del parámetro | varchar(50) | 50 | No | PK |  |  |  |
| valor | Valor del parámetro; NULL = regla pendiente | varchar(50) | 50 | Sí |  |  |  |  |
| descripcion | Qué significa el parámetro | varchar(200) | 200 | Sí |  |  |  |  |

### `detalle_pago`

_Renglones del ticket de pago tal como se calcularon_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_detalle | Identificador del renglón | int | 10 | No | PK |  |  | auto_increment |
| id_pago | Pago al que pertenece el renglón | int | 10 | No |  | pago_semanal.id_pago (fk_detalle_pago) |  |  |
| fecha | Día del evento; NULL en conceptos semanales | date |  | Sí |  |  |  |  |
| concepto | Tipo de renglón del ticket | enum('COMIDA_EXCESO','RETARDO','RETARDO_ACUMULADO','SALIDA_TEMPRANA','FALTA_INJUSTIFICADA','TIEMPO_EXTRA') | 19 | No |  |  |  |  |
| minutos | Minutos del evento | int | 10 | No |  |  | 0 |  |
| monto | Negativo = descuento, 0 = informativo, positivo = pago | decimal(10,2) | 10,2 | No |  |  | 0.00 |  |

### `empleado`

_Personas que checan asistencia y reciben pago semanal_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_empleado | Identificador del empleado | int | 10 | No | PK |  |  | auto_increment |
| id_puesto | Puesto que ocupa el empleado | int | 10 | No |  | puesto.id_puesto (fk_empleado_puesto) |  |  |
| nombre | Nombre(s) del empleado | varchar(60) | 60 | No |  |  |  |  |
| apellido_paterno | Apellido paterno | varchar(60) | 60 | No |  |  |  |  |
| apellido_materno | Apellido materno (opcional) | varchar(60) | 60 | Sí |  |  |  |  |
| telefono | Teléfono de contacto | varchar(20) | 20 | Sí |  |  |  |  |
| fecha_ingreso | Fecha en que empezó a trabajar | date |  | No |  |  |  |  |
| sueldo_base_semanal | Sueldo semanal pactado, base del cálculo (48 h) | decimal(10,2) | 10,2 | No |  |  |  | ck_empleado_sueldo |
| clave_hash | Hash de la clave personal para checar (nunca texto plano) | varchar(255) | 255 | Sí |  |  |  |  |
| id_huella | Identificador en el lector de huella (uso futuro) | int | 10 | Sí |  |  |  | id_huella |
| activo | 1 = trabaja actualmente, 0 = baja | tinyint(1) | 3 | No |  |  | 1 |  |

### `horario_general`

_Horario oficial por día de la semana (lunes a sábado)_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| dia_semana | Día: 1 = lunes ... 6 = sábado (WEEKDAY + 1) | tinyint | 3 | No | PK |  |  | ck_horario_dia |
| hora_entrada | Hora oficial de entrada | time |  | No |  |  |  | ck_horario_horas |
| hora_salida | Hora oficial de salida | time |  | No |  |  |  | ck_horario_horas |

### `pago_semanal`

_Resumen congelado del pago de un empleado en una semana_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_pago | Identificador del pago | int | 10 | No | PK |  |  | auto_increment |
| id_empleado | Empleado al que se paga | int | 10 | No |  | empleado.id_empleado (fk_pago_empleado) |  | uq_pago_empleado_semana |
| id_semana | Semana que se paga | int | 10 | No |  | semana.id_semana (fk_pago_semana) |  | uq_pago_empleado_semana |
| id_usuario | Encargado que generó el pago | int | 10 | Sí |  | usuario.id_usuario (fk_pago_usuario) |  |  |
| sueldo_base_usado | Copia del sueldo base al momento de calcular | decimal(10,2) | 10,2 | No |  |  |  | ck_pago_montos |
| valor_hora | Sueldo base / 48 al momento de calcular | decimal(10,4) | 10,4 | No |  |  |  |  |
| min_retardo | Minutos de retardo de la semana | int | 10 | No |  |  | 0 |  |
| min_salida_temprana | Minutos de salida antes de hora | int | 10 | No |  |  | 0 |  |
| min_falta | Minutos de faltas injustificadas | int | 10 | No |  |  | 0 |  |
| min_exceso_comida | Minutos de comida de más | int | 10 | No |  |  | 0 |  |
| min_extra | Minutos de tiempo extra | int | 10 | No |  |  | 0 |  |
| descuento_total | Suma de renglones con monto negativo (positivo) | decimal(10,2) | 10,2 | No |  |  | 0.00 | ck_pago_montos |
| monto_extra | Suma pagada por tiempo extra | decimal(10,2) | 10,2 | No |  |  | 0.00 | ck_pago_montos |
| salario_final | Sueldo base - descuentos + extra, mínimo 0 | decimal(10,2) | 10,2 | No |  |  |  | ck_pago_montos |
| cerrado | 1 = pagado y congelado, 0 = borrador | tinyint(1) | 3 | No |  |  | 0 |  |
| fecha_generacion | Cuándo se calculó por última vez | datetime |  | No |  |  | CURRENT_TIMESTAMP | DEFAULT_GENERATED |
| observaciones | Nota del encargado (editable aun cerrado) | varchar(255) | 255 | Sí |  |  |  |  |

### `puesto`

_Catálogo de puestos de trabajo_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_puesto | Identificador del puesto | int | 10 | No | PK |  |  | auto_increment |
| nombre | Nombre del puesto (único) | varchar(50) | 50 | No |  |  |  | nombre |

### `registro_asistencia`

_Las cuatro checadas de un empleado en un día_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_registro | Identificador del registro diario | int | 10 | No | PK |  |  | auto_increment |
| id_empleado | Empleado que checó | int | 10 | No |  | empleado.id_empleado (fk_registro_empleado) |  | uq_registro_dia |
| fecha | Día laboral del registro | date |  | No |  |  |  | uq_registro_dia, ck_registro_fecha |
| entrada | Checada 1: entrada | datetime |  | Sí |  |  |  | ck_registro_fecha, ck_registro_orden |
| salida_comida | Checada 2: salida a comer | datetime |  | Sí |  |  |  | ck_registro_orden |
| regreso_comida | Checada 3: regreso de comer | datetime |  | Sí |  |  |  | ck_registro_orden |
| salida | Checada 4: salida | datetime |  | Sí |  |  |  | ck_registro_orden |
| metodo | Cómo se registró: Clave, Huella o Manual (corrección) | enum('Clave','Huella','Manual') | 6 | No |  |  | Clave |  |
| modificado_por | Usuario que corrigió la checada a mano | int | 10 | Sí |  | usuario.id_usuario (fk_registro_usuario) |  |  |

### `semana`

_Periodo de pago de lunes a sábado_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_semana | Identificador de la semana de pago | int | 10 | No | PK |  |  | auto_increment |
| fecha_inicio | Lunes de la semana (único) | date |  | No |  |  |  | fecha_inicio, ck_semana_fechas |
| fecha_fin | Sábado de la semana | date |  | No |  |  |  | ck_semana_fechas |

### `usuario`

_Personas que usan la página web (dueño y encargado)_

| Atributo | Significado | Tipo | Longitud | Nulo | PK | FK (referencia) | Por omisión | Restricciones / extra |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| id_usuario | Identificador del usuario de la página | int | 10 | No | PK |  |  | auto_increment |
| nombre_completo | Nombre de la persona | varchar(120) | 120 | No |  |  |  |  |
| nombre_usuario | Nombre para iniciar sesión (único) | varchar(50) | 50 | No |  |  |  | nombre_usuario |
| contrasena | Hash de la contraseña (nunca texto plano) | varchar(255) | 255 | No |  |  |  |  |
| rol | Rol: Dueno (dueño) o Encargado | enum('Dueno','Encargado') | 9 | No |  |  | Encargado |  |
| activo | 1 = puede entrar a la página, 0 = deshabilitado | tinyint(1) | 3 | No |  |  | 1 |  |

## 6.3 Restricciones CHECK

| tabla | restriccion | expresion |
| --- | --- | --- |
| empleado | ck_empleado_sueldo | (`sueldo_base_semanal` > 0) |
| horario_general | ck_horario_dia | (`dia_semana` between 1 and 6) |
| horario_general | ck_horario_horas | (`hora_salida` > `hora_entrada`) |
| pago_semanal | ck_pago_montos | ((`sueldo_base_usado` > 0) and (`descuento_total` >= 0) and (`monto_extra` >= 0) and (`salario_final` >= 0)) |
| registro_asistencia | ck_registro_fecha | ((`entrada` is null) or (cast(`entrada` as date) = `fecha`)) |
| registro_asistencia | ck_registro_orden | (((`entrada` is null) or (`salida_comida` is null) or (`salida_comida` >= `entrada`)) and ((`salida_comida` is null) or (`regreso_comida` is null) or (`regreso_comida` >= `salida_comida`)) and ((`regreso_comida` is null) or (`salida` is null) or (`salida` >= `regreso_comida`))) |
| semana | ck_semana_fechas | (`fecha_fin` > `fecha_inicio`) |

## 6.4 Llaves foráneas (integridad referencial)

| tabla | llave_foranea | tabla_referenciada | al_actualizar | al_borrar |
| --- | --- | --- | --- | --- |
| ausencia | fk_ausencia_empleado | empleado | CASCADE | RESTRICT |
| detalle_pago | fk_detalle_pago | pago_semanal | CASCADE | RESTRICT |
| empleado | fk_empleado_puesto | puesto | CASCADE | RESTRICT |
| pago_semanal | fk_pago_empleado | empleado | CASCADE | RESTRICT |
| pago_semanal | fk_pago_semana | semana | CASCADE | RESTRICT |
| pago_semanal | fk_pago_usuario | usuario | CASCADE | RESTRICT |
| registro_asistencia | fk_registro_empleado | empleado | CASCADE | RESTRICT |
| registro_asistencia | fk_registro_usuario | usuario | CASCADE | RESTRICT |

## 6.5 Índices

`PRIMARY` = llave primaria; los `uq_`/`UNIQUE` son claves candidatas; los `fk_` los crea MySQL para las llaves foráneas; los `idx_` se agregaron para búsquedas frecuentes.

| tabla | indice | columnas | Tipo |
| --- | --- | --- | --- |
| auditoria_registro | idx_auditoria_registro | id_registro | No único |
| auditoria_registro | PRIMARY | id_auditoria | Único |
| ausencia | idx_ausencia_fecha | fecha | No único |
| ausencia | PRIMARY | id_ausencia | Único |
| ausencia | uq_ausencia_dia | id_empleado, fecha | Único |
| configuracion | PRIMARY | parametro | Único |
| detalle_pago | idx_detalle_pago_concepto | id_pago, concepto | No único |
| detalle_pago | PRIMARY | id_detalle | Único |
| empleado | fk_empleado_puesto | id_puesto | No único |
| empleado | id_huella | id_huella | Único |
| empleado | idx_empleado_nombre | apellido_paterno, nombre | No único |
| empleado | PRIMARY | id_empleado | Único |
| horario_general | PRIMARY | dia_semana | Único |
| pago_semanal | fk_pago_semana | id_semana | No único |
| pago_semanal | fk_pago_usuario | id_usuario | No único |
| pago_semanal | PRIMARY | id_pago | Único |
| pago_semanal | uq_pago_empleado_semana | id_empleado, id_semana | Único |
| puesto | nombre | nombre | Único |
| puesto | PRIMARY | id_puesto | Único |
| registro_asistencia | fk_registro_usuario | modificado_por | No único |
| registro_asistencia | idx_registro_fecha | fecha | No único |
| registro_asistencia | PRIMARY | id_registro | Único |
| registro_asistencia | uq_registro_dia | id_empleado, fecha | Único |
| semana | fecha_inicio | fecha_inicio | Único |
| semana | PRIMARY | id_semana | Único |
| usuario | nombre_usuario | nombre_usuario | Único |
| usuario | PRIMARY | id_usuario | Único |

## 6.6 Triggers

Lógica completa de cada trigger: `Sql/03_rutinas.sql` y sección 10.6.

| tabla | trigger | momento | evento |
| --- | --- | --- | --- |
| ausencia | trg_ausencia_bi_cerrado | BEFORE | INSERT |
| ausencia | trg_ausencia_bi_checada | BEFORE | INSERT |
| ausencia | trg_ausencia_bu_cerrado | BEFORE | UPDATE |
| ausencia | trg_ausencia_bu_checada | BEFORE | UPDATE |
| ausencia | trg_ausencia_bd_cerrado | BEFORE | DELETE |
| detalle_pago | trg_detalle_bi_cerrado | BEFORE | INSERT |
| detalle_pago | trg_detalle_bu_cerrado | BEFORE | UPDATE |
| detalle_pago | trg_detalle_bd_cerrado | BEFORE | DELETE |
| pago_semanal | trg_pago_bu_cerrado | BEFORE | UPDATE |
| pago_semanal | trg_pago_bd_cerrado | BEFORE | DELETE |
| registro_asistencia | trg_registro_bi_ausencia | BEFORE | INSERT |
| registro_asistencia | trg_registro_bi_cerrado | BEFORE | INSERT |
| registro_asistencia | trg_registro_bu_ausencia | BEFORE | UPDATE |
| registro_asistencia | trg_registro_bu_cerrado | BEFORE | UPDATE |
| registro_asistencia | trg_registro_bd_cerrado | BEFORE | DELETE |
| registro_asistencia | trg_registro_au_auditoria | AFTER | UPDATE |

<div class="salto"></div>

# 7. Anomalías y normalización

## 7.1 Esquema inicial: la hoja que hoy se lleva a mano

Hoy el encargado usa una hoja por empleado y semana. Pasada a una sola tabla queda así:

```text
HOJA_SEMANAL( empleado, telefono, puesto, sueldo_base, semana_inicio, semana_fin,
              lun_entrada, lun_sal_comida, lun_reg_comida, lun_salida,
              mar_entrada, … , sab_salida,                 ← 24 columnas de checadas
              faltas,                                       ← texto: "jue injustificada; mar salud"
              horario_lv, horario_sab,                      ← copiado en cada hoja
              total_retardos, total_descuentos, total_extra, salario_final, firmado_por )
```

**No está en 1FN:**
- Tiene **grupos repetidos**: los 24 campos lunes…sábado son la misma información repetida por día.
- Tiene **atributos multivaluados**: `faltas` junta varias ausencias en un solo texto.

**Anomalías con datos reales del negocio:**

| Anomalía | Ejemplo |
| -------- | ------- |
| **Inserción** | No se puede registrar a Carla Mendoza (ingresó el 2026-11-09) hasta que tenga una hoja semanal. Tampoco se puede dar de alta un puesto nuevo sin un empleado. |
| **Actualización** | Si cambia el sueldo de Juan, o el nombre del puesto "Repartidor", hay que corregir **todas** sus hojas. Si se corrige una sí y otra no, quedan valores contradictorios. Lo mismo pasa si el dueño cambia el horario del sábado. |
| **Eliminación** | Si se tira la única hoja de una semana, se pierde también el teléfono y el puesto del empleado. |
| **Inconsistencia de derivados** | `salario_final` se escribe a mano y puede no coincidir con las checadas (no hay forma de comprobarlo). |

## 7.2 Primera forma normal (1FN)

Se eliminan los grupos repetidos y el texto multivaluado: queda **una fila por empleado y día**.

```text
ASISTENCIA_1FN( id_empleado, fecha,
                nombre, telefono, id_puesto, puesto, sueldo_base,
                semana_inicio, semana_fin, dia_semana, hora_entrada_oficial, hora_salida_oficial,
                entrada, salida_comida, regreso_comida, salida, tipo_ausencia,
                total_descuentos, total_extra, salario_final )
PK = (id_empleado, fecha)
```

**Dependencias funcionales (DF)** observadas en el negocio:

| DF | Significado |
| -- | ----------- |
| `id_empleado → nombre, telefono, id_puesto, sueldo_base` | Datos de la persona |
| `id_puesto → puesto` | Nombre del puesto |
| `fecha → semana_inicio, dia_semana` | Cada fecha cae en una semana y en un día |
| `semana_inicio → semana_fin` | La semana termina el sábado |
| `dia_semana → hora_entrada_oficial, hora_salida_oficial` | Horario único |
| `(id_empleado, fecha) → entrada, salida_comida, regreso_comida, salida, tipo_ausencia` | Lo que pasó ese día |
| `(id_empleado, semana_inicio) → total_descuentos, total_extra, salario_final` | El pago es semanal |

## 7.3 Segunda forma normal (2FN)

Hay **dependencias parciales**: atributos que dependen solo de una parte de la PK
(`id_empleado` o `fecha`). Se separan:

```text
EMPLEADO( id_empleado, nombre, telefono, id_puesto, puesto, sueldo_base )
DIA( fecha, semana_inicio, semana_fin, dia_semana, hora_entrada_oficial, hora_salida_oficial )
REGISTRO( id_empleado, fecha, entrada, salida_comida, regreso_comida, salida, tipo_ausencia )
PAGO( id_empleado, semana_inicio, total_descuentos, total_extra, salario_final )
```

## 7.4 Tercera forma normal (3FN)

Se quitan las **dependencias transitivas**:

- En `EMPLEADO`: `id_empleado → id_puesto → puesto`. Sale **`puesto(id_puesto, nombre)`**.
- En `DIA`: `fecha → dia_semana → horas oficiales` y `fecha → semana_inicio → semana_fin`. Salen
  **`horario_general(dia_semana, hora_entrada, hora_salida)`** y
  **`semana(id_semana, fecha_inicio, fecha_fin)`**. `DIA` ya no hace falta como tabla, porque
  `dia_semana = WEEKDAY(fecha) + 1` se calcula.
- En `REGISTRO`: `tipo_ausencia` solo tiene valor los días **sin** checadas, y un día puede tener
  varios hechos distintos. Para no llenar columnas de NULL y no mezclar dos hechos, se separa
  **`ausencia(id_empleado, fecha, tipo, observaciones)`**.
- En `PAGO`: los totales salen de los renglones del cálculo. Se separa
  **`detalle_pago(id_pago, fecha, concepto, minutos, monto)`**, y los totales se conservan en
  `pago_semanal` como **desnormalización controlada** (§7.6).
- Los valores que el dueño puede cambiar (48 h, 5 min, 30 min, 540 min, ×2, ×3) pasan a
  **`configuracion`**, en lugar de repetirse en cada cálculo.
- Se agregan `usuario` (quién usa la página) y `auditoria_registro` (quién corrigió qué).

**Resultado:** el esquema final de 11 tablas en `Sql/01_esquema.sql`. No hubo que hacerle cambios
de normalización: **ya cumple 3FN** salvo las desnormalizaciones controladas que se justifican
abajo. No se inventaron errores en el diseño final.

## 7.5 Revisión del esquema final: BCNF, 4FN y 5FN

**BCNF** (todo determinante de una DF no trivial es superllave). Se revisó tabla por tabla:

| Tabla | ¿BCNF? | Comentario |
| ----- | ------ | ---------- |
| `puesto`, `usuario`, `horario_general`, `configuracion`, `ausencia` | Sí | Sus únicos determinantes son la PK o una clave candidata `UNIQUE`. |
| `empleado` | Sí | `id_huella` determina todo, pero es clave candidata (`UNIQUE`). |
| `semana` | Sí, si se reconoce `fecha_fin` como clave candidata | `fecha_inicio → fecha_fin` y `fecha_fin → fecha_inicio`. Ambas son claves; solo `fecha_inicio` está declarada `UNIQUE`. `fecha_fin` además es derivable (`fecha_inicio + 5 días`) y se guarda para que las consultas `BETWEEN` sean simples. |
| `registro_asistencia` | **No estricta** | Por `ck_registro_fecha` existe la DF `entrada → fecha` (`DATE(entrada) = fecha`), y `entrada` no es superllave (dos empleados pueden entrar el mismo minuto). Como `fecha` es **atributo primo** (forma parte de `id_empleado, fecha`), la tabla **sí cumple 3FN** pero no BCNF. Se acepta porque `fecha` hace falta como llave aunque `entrada` sea NULL (correcciones manuales), y el `CHECK` impide la anomalía. |
| `pago_semanal` | **No** (desnormalización controlada) | `sueldo_base_usado → valor_hora` es transitiva, y los totales (`descuento_total`, `monto_extra`, `min_*`, `salario_final`) se derivan de `detalle_pago`. Ver §7.6. |
| `detalle_pago` | Sí dentro de la tabla | `monto` se deriva de `minutos` y del `valor_hora` del pago (otra tabla); es parte de la misma "foto". |
| `auditoria_registro` | **No** (a propósito) | `id_registro → id_empleado, fecha` es transitiva. La copia es intencional: la bitácora debe seguir legible si el registro se borra, y por eso no tiene FK. |

**4FN** (sin dependencias multivaluadas no triviales). En la hoja original había dos hechos
multivaluados **independientes** por empleado: sus checadas y sus ausencias (y el pago). En el
esquema final viven en tablas separadas (`registro_asistencia`, `ausencia`, `pago_semanal`), así
que ninguna tabla mezcla dos listas independientes. **Cumple 4FN.**

**5FN** (sin dependencias de reunión no implicadas por las llaves). No hay relaciones ternarias:
`pago_semanal` asocia solo empleado y semana, y sus demás atributos dependen de esa pareja.
Descomponerla en proyecciones y volver a unirlas no recupera nada nuevo, y generaría tuplas falsas
si se separaran los montos. **Cumple 5FN.** No conviene descomponer más.

## 7.6 Desnormalización controlada de `pago_semanal` y `detalle_pago`

Los totales y renglones del pago se **guardan** en lugar de calcularse cada vez que se consultan.
Es intencional:

1. **Foto histórica.** El ticket justifica lo que se pagó. Si mañana cambia el sueldo de Juan, la
   tolerancia o el multiplicador del tiempo extra, el pago de la semana 1 debe seguir diciendo
   $2,807.28. Por eso se guardan `sueldo_base_usado` y `valor_hora`, aunque se puedan calcular.
2. **Ticket congelado.** Al cerrar el pago, 11 triggers impiden cambiar checadas, ausencias,
   renglones y montos de esa semana. Que el dato derivado sea inmutable elimina la anomalía de
   actualización.
3. **Una sola fuente que escribe.** Solo `sp_generar_pago` escribe totales y renglones, dentro de
   una transacción; los totales son la suma de los renglones. La app no recalcula.
4. **Rendimiento.** La pantalla principal del encargado (`v_resumen_pago_semanal`) lee un renglón
   por pago, en vez de recalcular minutos de cada checada de la semana.

<div class="salto"></div>

# 8. Álgebra relacional

Cada expresión se comprobó con su SQL equivalente (`Sql/algebra_relacional.sql`) en **MySQL 8.0.46**
el 2026-11-16, sobre la base recién reconstruida con `Sql/01–04` (datos ficticios). Los
resultados de esta página son los que **devolvió MySQL**, no cálculos a mano.

Notación: σ selección · π proyección · ⋈ reunión (join) · ∪ unión · − diferencia ·
∩ intersección · γ agrupación con funciones de agregado.

Relaciones usadas (atributos principales):
`empleado(id_empleado, id_puesto, nombre, apellido_paterno, …, sueldo_base_semanal)`,
`puesto(id_puesto, nombre)`, `ausencia(id_ausencia, id_empleado, fecha, tipo)`,
`pago_semanal(id_pago, id_empleado, id_semana, …, salario_final)`,
`detalle_pago(id_detalle, id_pago, fecha, concepto, minutos, monto)`.

---

### AR1 — Selección (σ)

**Pregunta:** ¿qué empleados ganan más de $3,000 a la semana?

```text
σ sueldo_base_semanal > 3000 (empleado)
```

```sql
SELECT DISTINCT * FROM empleado WHERE sueldo_base_semanal > 3000;
```

**Resultado observado (2 tuplas):**

| id_empleado | nombre | apellido_paterno | sueldo_base_semanal |
| ----------- | ------ | ---------------- | ------------------- |
| 2 | Juan | Pérez | 3300.00 |
| 4 | Luis | Ramírez | 3150.00 |

(MySQL devolvió todas las columnas de `empleado`; aquí se muestran las relevantes.)

### AR2 — Proyección sobre una selección (π, σ)

**Pregunta:** ¿cómo se llaman los floristas (puesto 3)?

```text
π nombre, apellido_paterno ( σ id_puesto = 3 (empleado) )
```

```sql
SELECT DISTINCT nombre, apellido_paterno FROM empleado WHERE id_puesto = 3;
```

**Resultado observado (2 tuplas):** (Ana, Torres), (Luis, Ramírez).

### AR3 — Reunión (⋈)

**Pregunta:** ¿qué puesto tiene cada empleado?

```text
π empleado.nombre, empleado.apellido_paterno, puesto.nombre
   ( empleado ⋈ empleado.id_puesto = puesto.id_puesto  puesto )
```

```sql
SELECT DISTINCT e.nombre, e.apellido_paterno, p.nombre AS puesto
FROM empleado e INNER JOIN puesto p ON e.id_puesto = p.id_puesto;
```

**Resultado observado (5 tuplas):**

| nombre | apellido_paterno | puesto |
| ------ | ---------------- | ------ |
| Ana | Torres | Florista |
| Luis | Ramírez | Florista |
| María | López | Recepcionista |
| Carla | Mendoza | Recepcionista |
| Juan | Pérez | Repartidor |

### AR4 — Selección, reunión y proyección

**Pregunta:** ¿quién tuvo faltas injustificadas y en qué fecha?

```text
π nombre, apellido_paterno, fecha
   ( σ tipo = 'Injustificada' (ausencia) ⋈ ausencia.id_empleado = empleado.id_empleado empleado )
```

```sql
SELECT DISTINCT e.nombre, e.apellido_paterno, a.fecha
FROM (SELECT * FROM ausencia WHERE tipo = 'Injustificada') a
INNER JOIN empleado e ON a.id_empleado = e.id_empleado;
```

**Resultado observado (2 tuplas):** (Juan, Pérez, 2026-10-29), (Ana, Torres, 2026-11-04).

### AR5 — Diferencia (−)

**Pregunta:** ¿qué empleados todavía no tienen ningún pago generado?

```text
π id_empleado (empleado)  −  π id_empleado (pago_semanal)
```

```sql
SELECT id_empleado FROM empleado
EXCEPT
SELECT id_empleado FROM pago_semanal;       -- EXCEPT existe desde MySQL 8.0.31
```

**Resultado observado (1 tupla):** {5} → Carla Mendoza, que ingresó el 2026-11-09, después de
las dos semanas de prueba.

### AR6 — Unión (∪)

**Pregunta:** ¿qué empleados tuvieron alguna incidencia (ausencia registrada o retardo)?

```text
π id_empleado (ausencia)
   ∪
π id_empleado ( σ concepto = 'RETARDO' (detalle_pago) ⋈ detalle_pago.id_pago = pago_semanal.id_pago pago_semanal )
```

```sql
SELECT id_empleado FROM ausencia
UNION
SELECT p.id_empleado
FROM detalle_pago d INNER JOIN pago_semanal p ON d.id_pago = p.id_pago
WHERE d.concepto = 'RETARDO';
```

**Resultado observado (4 tuplas):** {1, 2, 3, 4}. Los dos operandos también se consultaron por
separado: con ausencia, {2, 3, 4}; con retardo, {1, 2} (María el 2026-11-04 con 10 min; Juan el
27, 28 y 30 de octubre con 12, 7 y 20 min). La unión elimina el 2 repetido.

### AR7 — Agrupación con agregados (γ)

**Pregunta:** ¿cuánto se paga en total cada semana y a cuántas personas?

```text
id_semana γ SUM(salario_final) → total, COUNT(id_pago) → pagos (pago_semanal)
```

```sql
SELECT id_semana, SUM(salario_final) AS total, COUNT(id_pago) AS pagos
FROM pago_semanal GROUP BY id_semana;
```

**Resultado observado (2 tuplas):**

| id_semana | total | pagos |
| --------- | ----- | ----- |
| 1 | 11868.73 | 4 |
| 2 | 14562.50 | 4 |

Coincide con los totales de regresión del proyecto.

### AR8 — Intersección (∩)

**Pregunta:** ¿quiénes tienen pago en la semana 1 **y** en la semana 2?

```text
π id_empleado ( σ id_semana = 1 (pago_semanal) )  ∩  π id_empleado ( σ id_semana = 2 (pago_semanal) )
```

```sql
SELECT id_empleado FROM pago_semanal WHERE id_semana = 1
INTERSECT
SELECT id_empleado FROM pago_semanal WHERE id_semana = 2;   -- INTERSECT desde MySQL 8.0.31
```

**Resultado observado (4 tuplas):** {1, 2, 3, 4}.

<div class="salto"></div>

# 9. Consultas SQL explicadas

Las 10 consultas están en `Sql/04_consultas_vistas.sql`. Los resultados son los que devolvió
**MySQL 8.0.46** el 2026-11-16, justo después de reconstruir con `01–04`. Las columnas de fecha y
hora de generación cambian con cada ejecución.

| # | Técnica | Pregunta del negocio | Resultado observado |
| - | ------- | -------------------- | ------------------- |
| 1 | `INNER JOIN` | ¿Qué empleados hay, en qué puesto y con qué sueldo? | 5 filas: Luis (Florista, 3150), Ana (Florista, 3000), María (Recepcionista, 3000), Carla (Recepcionista, 3000), Juan (Repartidor, 3300) |
| 2 | `JOIN` de 4 tablas + filtro | ¿Qué descuentos tuvo cada empleado en la semana 1 y de qué día? | 7 filas: María, 4 de comida de más (−8.33, −15.63, −22.92, −41.67); Juan, comida −11.46, falta −584.38 y salida temprana −34.38 |
| 3 | `LEFT JOIN` + `COUNT` | ¿Cuántos pagos tiene cada empleado, incluso quien no tiene ninguno? | María, Juan, Ana y Luis: 2 cada uno; **Carla: 0** (aparece gracias al `LEFT JOIN`) |
| 4 | Subconsulta escalar (`AVG`) | ¿Quién cobró más que el promedio en la semana 2? | Juan 5156.25 y Luis 3937.50 (promedio de la semana: 3640.63) |
| 5 | `NOT EXISTS` | ¿Qué empleados activos nunca han checado? | Carla Mendoza (ingresó el 2026-11-09) |
| 6 | `SUM` + `GROUP BY` + `HAVING` (sobre vista) | ¿Quién acumula retardos o comida de más? | María: 10 min de retardo y 85 de comida de más; Juan: 39 y 10 |
| 7 | `COUNT`, `SUM`, `AVG`, `MAX` + `GROUP BY` | ¿Cuánto se paga, descuenta y agrega cada semana? | Semana 1: 4 pagos, total 11868.73, promedio 2967.18, máximo 3150.00, descuentos 718.77, extra 137.50. Semana 2: 4 pagos, 14562.50, 3640.63, 5156.25, 531.25, 2643.75 |
| 8 | `COUNT` + `GROUP BY` + `HAVING` | ¿Qué puestos tienen incidencias con descuento? | Repartidor: 3 incidencias, 550 min, −630.22; Florista: 1, 510, −531.25; Recepcionista: 4, 85, −88.55 |
| 9 | Función de ventana `RANK() OVER (PARTITION BY …)` | ¿Quién fue más puntual cada semana? | Semana 1: Ana y Luis 1.º (0 min), Juan 3.º (79), María 4.ª (85). Semana 2: Juan, Ana y Luis 1.º (0), María 4.ª (10) |
| 10 | Tabla cruzada `SUM(CASE …)` | ¿Cómo queda cada ticket por concepto en una fila? | Ej.: Juan semana 1 → comida −11.46, salida −34.38, faltas −584.38, extra 137.50, **neto −492.72** (= −630.22 + 137.50) |

**Vistas** (también en `04`):

- `v_asistencia_diaria`: una fila por empleado y día, con horas, retardo, comida de más, salida
  temprana y extra. Base del reporte de asistencia.
- `v_resumen_pago_semanal`: el pago de cada empleado por semana con su estado. Con los datos de
  prueba devuelve los 8 pagos de la regresión.
- `v_dias_sin_checada`: días laborales sin checada ni ausencia. Con los datos de prueba devuelve
  **0 filas**, porque cada falta ya tiene su ausencia registrada; la prueba P19 demuestra que
  detecta 29 días cuando se agrega una semana sin checadas.

<div class="salto"></div>

# 10. Implementación en MySQL 8.0

**Entorno:** MySQL Community Server 8.0.46 (Windows 11), InnoDB, `utf8mb4` /
`utf8mb4_unicode_ci`, MySQL Workbench 8.0. Scripts en `Sql/`, ejecutados en orden
`01 → 02 → 03 → 04` (y `05` para el usuario de la app). Todo se reconstruye desde cero porque
`01_esquema.sql` empieza con `DROP DATABASE IF EXISTS`.

| Objeto | Cantidad | Dónde |
| ------ | -------- | ----- |
| Tablas | 11 | `01_esquema.sql` |
| Restricciones `CHECK` con nombre | 7 | `01_esquema.sql` |
| Llaves foráneas con nombre | 8 (todas `ON UPDATE CASCADE` / `ON DELETE RESTRICT`) | `01_esquema.sql` |
| Índices | 27 (PK, `UNIQUE`, FK y 5 `idx_` de búsqueda) | `01_esquema.sql` |
| Funciones | 3 | `03_rutinas.sql` |
| Procedimientos | 5 | `03_rutinas.sql` |
| Triggers | 16 | `03_rutinas.sql` |
| Vistas | 3 | `04_consultas_vistas.sql` |
| Consultas documentadas | 10 | `04_consultas_vistas.sql` |

## 10.1 Tablas y restricciones

Cada tabla y columna tiene un `COMMENT` con su significado; el diccionario (sección 6) se genera
de ahí. Restricciones `CHECK`:

| Restricción | Regla |
| ----------- | ----- |
| `ck_empleado_sueldo` | `sueldo_base_semanal > 0` |
| `ck_horario_dia` | `dia_semana BETWEEN 1 AND 6` (lunes a sábado) |
| `ck_horario_horas` | `hora_salida > hora_entrada` |
| `ck_registro_fecha` | `entrada IS NULL OR DATE(entrada) = fecha` |
| `ck_registro_orden` | entrada ≤ salida a comer ≤ regreso ≤ salida |
| `ck_semana_fechas` | `fecha_fin > fecha_inicio` |
| `ck_pago_montos` | sueldo > 0; descuento, extra y salario ≥ 0 |

Llaves únicas que implementan claves candidatas: `uq_registro_dia`, `uq_ausencia_dia`,
`uq_pago_empleado_semana`, más `UNIQUE` en `puesto.nombre`, `usuario.nombre_usuario`,
`empleado.id_huella` y `semana.fecha_inicio`.

## 10.2 Integridad de entidad, referencial y de dominio

**Integridad de entidad.** Cada fila debe poder identificarse sin ambigüedad.
- Las 11 tablas tienen **PRIMARY KEY** (no nula y única por definición). Nueve usan una llave
  sustituta `INT AUTO_INCREMENT`; `horario_general` (`dia_semana`) y `configuracion` (`parametro`)
  usan una llave natural.
- Las **llaves naturales** se protegen con `UNIQUE`, incluidas tres **compuestas**:
  `uq_registro_dia (id_empleado, fecha)`, `uq_ausencia_dia (id_empleado, fecha)` y
  `uq_pago_empleado_semana (id_empleado, id_semana)`. Así una PK sustituta no permite duplicar el
  mismo hecho (evidencia: P15b, `ERROR 1062 … uq_registro_dia`).
- *Justificación:* el pago se calcula por empleado y día. Dos registros del mismo día duplicarían
  descuentos o extra.

**Integridad referencial.** Toda referencia apunta a una fila que existe.
- 8 **FOREIGN KEY** con nombre (`fk_…`), todas con `ON UPDATE CASCADE` y `ON DELETE RESTRICT`.
- `RESTRICT`: no se puede borrar un empleado, puesto, semana o pago que tenga historial, porque
  se perdería la justificación de pagos ya hechos (P05, `ERROR 1451`). La baja normal es `activo = 0`.
- `CASCADE` al actualizar: si cambiara una llave, las referencias la siguen. Con llaves sustitutas
  casi no ocurre.
- FK **opcionales** (admiten NULL) donde el negocio lo permite: `registro_asistencia.modificado_por`
  (solo en correcciones) y `pago_semanal.id_usuario`.
- Excepción **justificada:** `auditoria_registro` no tiene FK a propósito, para que la bitácora
  sobreviva aunque el registro auditado se borre.
- Evidencia: P15a y P20 (`ERROR 1452`: no se acepta un empleado o un usuario inexistente).

**Integridad de dominio.** Cada valor pertenece a su conjunto válido.
- **Tipos adecuados:** `DECIMAL(10,2)` para dinero (sin errores de redondeo binario), `DATE`,
  `TIME` y `DATETIME` para el tiempo, `BOOLEAN` para banderas, y `ENUM` para listas cerradas
  (`rol`, `metodo`, `tipo` de ausencia, `concepto` del ticket).
- **NOT NULL** en todo lo obligatorio; los opcionales están justificados en §4.2.
- **DEFAULT:** `activo = 1`, `cerrado = 0`, `metodo = 'Clave'`, `rol = 'Encargado'`, minutos y
  montos en 0, `fecha_generacion` y `fecha_cambio` con `CURRENT_TIMESTAMP`.
- **CHECK** (7, ver §10.1): sueldo > 0, días 1–6, salida > entrada, fecha de la checada, orden de
  las 4 checadas, fin de semana > inicio y montos no negativos (P04, P15c y P15d, `ERROR 3819`).
- **Reglas de dominio que dependen de otras filas** (no caben en un `CHECK`): se hacen cumplir
  con triggers (semana cerrada; checada o ausencia, no ambas) y con `sp_registrar_checada`
  (domingo, hora anterior, máximo 4 checadas).

## 10.3 Funciones

| Función | Qué calcula |
| ------- | ----------- |
| `fn_valor_hora(sueldo)` | sueldo ÷ `horas_semana_base` (48); error si falta el parámetro |
| `fn_minutos_comida_extra(salida, regreso)` | minutos de comida por encima de `comida_min` (30) |
| `fn_semana_cerrada(empleado, fecha)` | 1 si el pago de esa semana ya está cerrado (la usan los triggers) |

## 10.4 Procedimientos almacenados

| Procedimiento | Qué hace |
| ------------- | -------- |
| `sp_registrar_checada` | Decide cuál de las 4 checadas sigue y la guarda; valida empleado activo, método, domingo, orden y máximo 4 |
| `sp_generar_pago` | Calcula retardos, comida de más, salida temprana, faltas injustificadas, tiempo extra (×2 hasta 540 min y ×3 después, con `SUM() OVER`) y retardos acumulados (si el dueño define la regla); guarda pago y renglones **en una transacción** |
| `sp_ticket` | Devuelve el resumen y los renglones guardados (no recalcula) |
| `sp_cerrar_pago` | Congela el pago |
| `sp_agregar_observacion` | Nota del encargado, permitida aun con el pago cerrado |

## 10.5 Transacciones

`sp_generar_pago` ejecuta `START TRANSACTION … COMMIT` con
`DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END`. Si algo falla a la mitad,
por ejemplo un usuario inexistente que viola `fk_pago_usuario`, se deshace todo, incluso el
borrado de renglones que ya se había hecho (prueba **P20**: error 1452 y el pago 8 queda igual).
Las pruebas **P11** (COMMIT), **P12** (ROLLBACK manual) y **P19** (ROLLBACK con vista) completan
la demostración. Limitación documentada (**INC-04**): el `START TRANSACTION` interno confirma lo
que el llamador tuviera abierto, por eso la app debe llamar el procedimiento en autocommit.

## 10.6 Triggers

Los 16 triggers documentan en `Sql/03_rutinas.sql` su evento, condición y efecto. Resumen:

| Trigger | Evento | Condición | Efecto |
| ------- | ------ | --------- | ------ |
| `trg_registro_bi_cerrado` | BEFORE INSERT en `registro_asistencia` | `fn_semana_cerrada(NEW.id_empleado, NEW.fecha) = 1` | Rechaza la checada (1644) |
| `trg_registro_bu_cerrado` | BEFORE UPDATE en `registro_asistencia` | La semana de la fila original **o** la de la nueva está cerrada | Rechaza el cambio (1644) |
| `trg_registro_bd_cerrado` | BEFORE DELETE en `registro_asistencia` | `fn_semana_cerrada(OLD.id_empleado, OLD.fecha) = 1` | Rechaza el borrado (1644) |
| `trg_registro_au_auditoria` | AFTER UPDATE en `registro_asistencia` | Cambió alguna de las 4 horas que **ya tenía valor** (llenar una vacía no se audita) | Inserta en `auditoria_registro` el antes, el después, `modificado_por` y `USER()` |
| `trg_ausencia_bi_cerrado` | BEFORE INSERT en `ausencia` | `fn_semana_cerrada(NEW.id_empleado, NEW.fecha) = 1` | Rechaza la ausencia (1644) |
| `trg_ausencia_bu_cerrado` | BEFORE UPDATE en `ausencia` | La semana original **o** la nueva está cerrada | Rechaza el cambio (1644) |
| `trg_ausencia_bd_cerrado` | BEFORE DELETE en `ausencia` | `fn_semana_cerrada(OLD.id_empleado, OLD.fecha) = 1` | Rechaza el borrado (1644) |
| `trg_detalle_bi_cerrado` | BEFORE INSERT en `detalle_pago` | El pago `NEW.id_pago` tiene `cerrado = 1` | Rechaza el renglón (1644) |
| `trg_detalle_bu_cerrado` | BEFORE UPDATE en `detalle_pago` | El pago `OLD.id_pago` tiene `cerrado = 1` | Rechaza el cambio (1644) |
| `trg_detalle_bd_cerrado` | BEFORE DELETE en `detalle_pago` | El pago `OLD.id_pago` tiene `cerrado = 1` | Rechaza el borrado (1644) |
| `trg_pago_bu_cerrado` | BEFORE UPDATE en `pago_semanal` | `OLD.cerrado = 1` y cambia cualquier columna **excepto** `observaciones` | Rechaza el cambio (1644) |
| `trg_pago_bd_cerrado` | BEFORE DELETE en `pago_semanal` | `OLD.cerrado = 1` | Rechaza el borrado (1644) |
| `trg_registro_bi_ausencia` | BEFORE INSERT en `registro_asistencia` | Existe una `ausencia` del mismo empleado en `NEW.fecha` | Rechaza la checada (1644) |
| `trg_registro_bu_ausencia` | BEFORE UPDATE en `registro_asistencia` | Cambia el empleado o la fecha **y** el nuevo día tiene ausencia | Rechaza el cambio (1644) |
| `trg_ausencia_bi_checada` | BEFORE INSERT en `ausencia` | Existe un `registro_asistencia` del mismo empleado en `NEW.fecha` | Rechaza la ausencia (1644) |
| `trg_ausencia_bu_checada` | BEFORE UPDATE en `ausencia` | Cambia el empleado o la fecha **y** el nuevo día tiene checadas | Rechaza el cambio (1644) |

**Sin doble ejecución:** los triggers solo validan y auditan; los cálculos los hacen únicamente
los procedimientos. La aplicación no recalcula pagos ni escribe la auditoría, y además no tiene
permiso para hacerlo (`Sql/CONTRATO_BD.md`, regla 0).

## 10.7 Seguridad y respaldo

- **Usuario de la app `app_floreria`** (`Sql/05_usuario_app.sql`): `EXECUTE` en rutinas,
  `SELECT` en vistas, DML solo donde el CRUD lo necesita y **solo lectura** en pagos y
  auditoría. No tiene `DROP`, `ALTER` ni `CREATE`, ni acceso a otras bases. Probado: 17/17 casos.
- **Credenciales:** ninguna contraseña en el repositorio. La app usa user-secrets o variables de
  entorno (`Sql/conexion_plantilla.md`). Contraseñas y claves se guardan como hash.
- **Respaldo:** `Sql/respaldo_restauracion.ps1` con `mysqldump --single-transaction --routines
  --triggers --events`. La restauración en una base aparte dio el mismo `CHECKSUM` en las 11
  tablas. Los respaldos no se suben a GitHub (`Sql/.gitignore`). Se recomienda una copia fuera de
  la computadora y probar la restauración periódicamente.

<div class="salto"></div>

# 11. Pruebas de base de datos

**Ejecución:** 2026-11-16 · MySQL Community Server 8.0.46 (Windows 11) · cliente `mysql` como
`root`, salvo donde se indica · base reconstruida con `Sql/01–04` y luego
`Pruebas/pruebas_bd.sql` **en orden**. Los casos *POR SEPARADO* se ejecutaron uno por uno, cada
uno en una conexión nueva.

Las pruebas se corrieron en una copia con otro nombre (`belissimo_prueba`) para no tocar la base
de trabajo; los scripts son los mismos.

**Evidencia (salida exacta del cliente):** `Pruebas/evidencias/salida_pruebas_bd_2026-11-16.txt`,
`salida_privilegios_2026-11-16.txt` y `salida_contrato_net_2026-11-16.txt`.

**Estados:** **Pasa** = lo observado coincide con lo esperado · **Pendiente** = no lo puede ejecutar
el DBA (lo hace el equipo de la aplicación).

## 11.1 Pruebas ejecutadas en el SGBD

| ID | Requisito / regla | Precondición | Entrada | Pasos | Esperado | Observado | Estado |
| -- | ----------------- | ------------ | ------- | ----- | -------- | --------- | ------ |
| P01 | RF02 Alta de empleado (RN02) | Existe el puesto 3 | Empleado "Prueba Temporal", sueldo 3200 | `INSERT` + `SELECT` | 1 fila insertada y visible | `Query OK, 1 row affected`; `SELECT` devolvió id 6, Prueba Temporal, 3200.00 | Pasa |
| P02 | RF02 Consulta con filtro y orden | P01 | Puesto Florista, orden por sueldo descendente | `SELECT … JOIN puesto … ORDER BY` | 3 filas: Prueba 3200, Luis 3150, Ana 3000 | 3 filas: Prueba Temporal 3200.00, Luis Ramírez 3150.00, Ana Torres 3000.00 | Pasa |
| P03 | RF02 Modificación | P01 | Teléfono nuevo 6461112233 | `UPDATE` + `SELECT` | 1 fila afectada con el nuevo teléfono | `Rows matched: 1 Changed: 1`; teléfono 6461112233 | Pasa |
| P04 | RN02 Validación negativa de dominio | P01 | Sueldo −100 | `UPDATE` (por separado) | Error 3819 `ck_empleado_sueldo` | `ERROR 3819 (HY000): Check constraint 'ck_empleado_sueldo' is violated.` | Pasa |
| P05 | RN15 Eliminación bloqueada por FK | María tiene checadas y pagos | `DELETE` empleado 1 | Por separado | Error 1451 | `ERROR 1451 (23000): Cannot delete or update a parent row: a foreign key constraint fails (… pago_semanal …)` | Pasa |
| P06 | RF02 Eliminación válida | Empleado de prueba sin historial | `DELETE` del empleado de prueba | `DELETE` + conteo | 1 fila borrada; conteo 0 | `Query OK, 1 row affected`; `filas_restantes = 0` | Pasa |
| P07 | RF01 / RN04 Procedimiento: 4 checadas | Carla sin checadas; lunes 2026-11-09 | 09:02, 13:00, 13:35, 17:30 con 'Clave' | 4 `CALL sp_registrar_checada` + `SELECT` | ENTRADA, SALIDA_COMIDA, REGRESO_COMIDA, SALIDA; 1 fila con 4 horas | Respondió exactamente esas 4 checadas; 1 fila (id_registro 45) con las 4 horas y método Clave | Pasa |
| P08 | RN04 Validación negativa del procedimiento | P07 | Quinta checada 18:00 | `CALL` (por separado) | Error 1644 "ya tiene las cuatro checadas" | `ERROR 1644 (45000): El empleado ya tiene las cuatro checadas de hoy.` | Pasa |
| P09 | RN03 / RN06 Funciones | — | `fn_valor_hora(3300)`, comida 13:00–13:35, `fn_semana_cerrada(1,'2026-10-26')` | `SELECT` | 68.7500, 5, 0 | 68.7500, 5, 0 | Pasa |
| P10 | RF05 / RF06 Procedimiento de pago y ticket | Pagos de 04 generados | Regenerar Juan semana 1 + `sp_ticket` | `CALL` ×2 | Descuento 630.22, extra 137.50, salario 2807.28 | Resumen: descuento 630.22, extra 137.50, salario 2807.28, Borrador; 7 renglones (comida −11.46, falta −584.38, salida −34.38, extra 137.50, 3 retardos en 0.00) | Pasa |
| P11 | Transacción con COMMIT | — | Teléfono de Ana 6469998877 | `START TRANSACTION; UPDATE; COMMIT;` + `SELECT` | Cambio guardado | Teléfono 6469998877 después del COMMIT | Pasa |
| P12 | Transacción con ROLLBACK | — | Sueldos 3500 y 3650 | `START TRANSACTION; UPDATE ×2; SELECT; ROLLBACK;` + `SELECT` | Dentro: 3500/3650; después: 3000/3150 | Dentro: 3500.00/3650.00; después del ROLLBACK: 3000.00/3150.00 | Pasa |
| P13 | RF04 / RN14 Trigger de auditoría | María entró 09:10 el 2026-11-04 | Corregir a 08:58, usuario 2, método Manual | `UPDATE` + `SELECT` auditoría + regenerar pago | 1 fila de auditoría 09:10 → 08:58; luego `min_retardo = 0` | 1 fila: anterior 09:10, nueva 08:58, `id_usuario_app = 2`, `usuario_bd = root@localhost` (se ejecutó como root); después `min_retardo = 0`, salario 3000.00 | Pasa |
| P14 | RF07 / RF08 Cerrar pago y observación | Pago 1 en borrador | `sp_cerrar_pago(1)` + observación | `CALL` ×2 + `SELECT` | Cerrado; observación guardada | `cerrado = 1`, salario 2911.45, observación guardada | Pasa |
| P14a | RN13 Regenerar pago cerrado | P14 | `sp_generar_pago(1,1,2)` | Por separado | Error 1644 | `ERROR 1644 (45000): El pago de esta semana ya esta cerrado y no se puede regenerar.` | Pasa |
| P14b | RN13 Corregir checada de semana cerrada | P14 | `UPDATE` salida 2026-10-26 | Por separado | Error 1644 (`trg_registro_bu_cerrado`) | `ERROR 1644 (45000): La semana de esa checada ya esta cerrada y no se puede modificar.` | Pasa |
| P14c | RN13 Insertar checada en semana cerrada | P14 | `INSERT` registro 2026-10-26 | Por separado | Error 1644 (`trg_registro_bi_cerrado`) | `ERROR 1644 (45000): La semana de esa checada ya esta cerrada y no se puede modificar.` | Pasa |
| P14d | RN13 Alterar renglón de ticket cerrado | P14 | `UPDATE detalle_pago` del pago 1 | Por separado | Error 1644 (`trg_detalle_bu_cerrado`) | `ERROR 1644 (45000): El pago esta cerrado: su ticket no se puede modificar.` | Pasa |
| P14e | RN13 Cambiar salario de pago cerrado | P14 | `UPDATE salario_final = 9999` | Por separado | Error 1644 (`trg_pago_bu_cerrado`) | `ERROR 1644 (45000): El pago esta cerrado: solo se pueden agregar observaciones.` | Pasa |
| P15a | Integridad: FK | — | Checada del empleado 999 | Por separado | Error 1452 | `ERROR 1452 (23000): … CONSTRAINT fk_registro_empleado …` | Pasa |
| P15b | RN04 Integridad: UNIQUE | P07 | Segundo registro de Carla el 2026-11-09 | Por separado | Error 1062 | `ERROR 1062 (23000): Duplicate entry '5-2026-11-09' for key 'registro_asistencia.uq_registro_dia'` | Pasa |
| P15c | RN04 Integridad: orden de checadas | — | Regreso de comer antes de la salida | Por separado | Falla `ck_registro_orden` | `ERROR 3819 (HY000): Check constraint 'ck_registro_orden' is violated.` | Pasa |
| P15d | RN04 Integridad: fecha | — | Entrada de otro día | Por separado | Falla `ck_registro_fecha` | `ERROR 3819 (HY000): Check constraint 'ck_registro_fecha' is violated.` | Pasa |
| P16 | RF10 Reporte y persistencia en el SGBD | P01–P15 | `v_resumen_pago_semanal` y conteos | `SELECT` en una **conexión nueva** | 8 pagos (pago 1 Cerrado), 1 corrección auditada, 1 registro de Carla con sus 4 checadas | 8 pagos, pago 1 Cerrado y el resto Borrador; `correcciones_auditadas = 1`; `checadas_de_carla = 1` (cuenta registros; el registro tiene las 4 horas, ver P07) | Pasa ¹ |
| P17 | RN13 Cerrar pago sin renglones | Pago 3 (Ana s1) sin renglones | `sp_cerrar_pago(3)` | `CALL` + `SELECT` | `cerrado = 1` | `cerrado = 1` | Pasa |
| P17a | RN13 Borrar pago cerrado (INC-03) | P17 | `DELETE` pago 3 | Por separado | Error 1644 (`trg_pago_bd_cerrado`) | `ERROR 1644 (45000): El pago esta cerrado: no se puede eliminar.` | Pasa |
| P17b | RN13 Cambiar empleado de pago cerrado (INC-02) | P14 | `UPDATE id_empleado = 5` del pago 1 | Por separado | Error 1644 | `ERROR 1644 (45000): El pago esta cerrado: solo se pueden agregar observaciones.` | Pasa |
| P18 | RF03 Ausencia en semana abierta | Ausencia de Ana 2026-11-04 | Cerrar pago 2; editar observación de Ana | `CALL` + `UPDATE` + `SELECT` | Cierre OK; 1 fila editada | `Query OK, 1 row affected` ×2; observación nueva visible | Pasa |
| P18a | RN13 Cambiar ausencia de semana cerrada | P18 | `UPDATE tipo` de Juan 2026-10-29 | Por separado | Error 1644 (`trg_ausencia_bu_cerrado`) | `ERROR 1644 (45000): La semana de esa ausencia ya esta cerrada y no se puede modificar.` | Pasa |
| P18b | RN13 Borrar ausencia de semana cerrada | P18 | `DELETE` ausencia de Juan 2026-10-29 | Por separado | Error 1644 (`trg_ausencia_bd_cerrado`) | `ERROR 1644 (45000): La semana de esa ausencia ya esta cerrada y no se puede modificar.` | Pasa |
| P19 | RF11 Vista de días sin checada + ROLLBACK | P07; hoy ≥ 2026-11-14 | Semana 9–14 nov dentro de una transacción | `START TRANSACTION; INSERT semana; SELECT vista; ROLLBACK;` | 29 días; después 0 | 29 días (María, Juan, Ana y Luis el lunes 9, 510 min); después del ROLLBACK, 0 | Pasa |
| P20 | RF05 ROLLBACK automático del procedimiento | Pago 8 en borrador con 2 renglones | `sp_generar_pago(4,2,999)` | `SELECT`; `CALL` por separado; `SELECT` | Error 1452; pago sin cambios | Antes: usuario 2, 3937.50, 2 renglones. `ERROR 1452 (23000): … CONSTRAINT fk_pago_usuario …`. Después: usuario 2, 3937.50, 2 renglones | Pasa |
| P21 | RN17 Ausencia en día sin checada (válido) | Carla sin checada el 2026-11-10 | Ausencia Justificada 2026-11-10 | `START TRANSACTION; INSERT; SELECT; ROLLBACK;` | Se inserta 1 fila | `Query OK, 1 row affected`; `SELECT` muestra (5, 2026-11-10, Justificada); ROLLBACK | Pasa |
| P21a | RN17 Checar en día con ausencia | Ana tiene falta el 2026-11-04 (semana 2 abierta) | `sp_registrar_checada(3,'2026-11-04 09:00','Clave')` | Por separado | Error 1644 (`trg_registro_bi_ausencia`) | `ERROR 1644 (45000): El empleado tiene una ausencia registrada ese dia: no puede tener checadas.` | Pasa |
| P21b | RN17 Ausencia en día con checadas | Luis checó el 2026-11-03 | `INSERT` ausencia (4, 2026-11-03) | Por separado | Error 1644 (`trg_ausencia_bi_checada`) | `ERROR 1644 (45000): El empleado ya tiene checadas ese dia: no se le puede registrar una ausencia.` | Pasa |
| P21c | RN17 Mover ausencia a día con checadas | Ana checó el 2026-11-05 | `UPDATE ausencia SET fecha='2026-11-05'` | Por separado | Error 1644 (`trg_ausencia_bu_checada`) | `ERROR 1644 (45000): El empleado ya tiene checadas ese dia: no se le puede registrar una ausencia.` | Pasa |
| P21d | RN17 Mover registro a día con ausencia | Ana tiene falta el 2026-11-04 | `UPDATE registro_asistencia SET fecha='2026-11-04'` (registro del 05) | Por separado | Error 1644 (`trg_registro_bu_ausencia`) | `ERROR 1644 (45000): El empleado tiene una ausencia registrada ese dia: no puede tener checadas.` | Pasa |

¹ En la primera versión del script, el esperado de P16 decía "las 4 checadas de Carla", pero la
consulta cuenta **registros**. Se corrigió el texto del esperado; la base se comportó bien.

## 11.2 Otras pruebas ejecutadas

| ID | Requisito | Entrada / pasos | Esperado | Observado | Estado |
| -- | --------- | --------------- | -------- | --------- | ------ |
| R01 | Regresión tras cada cambio | Reconstruir 01–04 y correr `verificacion_regresion.sql` | 22 comprobaciones en OK (objetos, 8 pagos, totales) | 22 OK, 0 FALLA, 0 advertencias (última corrida después de INC-05) | Pasa |
| S01 | Seguridad: usuario de mínimo privilegio | `05_usuario_app.sql` con un usuario temporal; 9 operaciones permitidas y 8 prohibidas | Permitidas funcionan; prohibidas dan 1142/1044 | 17/17 como se esperaba (UPDATE/INSERT/DELETE a pagos y auditoría, DROP, ALTER, CREATE, `mysql.user` y la base `veterinaria`, rechazados) | Pasa |
| S02 | Auditoría registra la cuenta real (INC-05) | Corrección de checada conectado como el usuario de la app | `usuario_bd` = cuenta de la app | `app_floreria_prueba@localhost` | Pasa |
| C01 | Contrato para la app (ADO.NET) | 9 llamadas de `CONTRATO_BD.md` con MySql.Data 26.10.0 en .NET 10 | Resultados y errores documentados | 9/9 OK (checada, error 1644 de domingo, pago, ticket con 2 resultados, vista, función, `bool`, 1644 al regenerar pago cerrado, 1142) | Pasa |
| B01 | Respaldo y restauración (base de prueba, antes de PEN-01) | `respaldo_restauracion.ps1` → base nueva → `CHECKSUM TABLE` | Mismas 11 tablas, 3 vistas, 8 rutinas, 12 triggers (los que había ese momento) y datos idénticos | 11/3/8/12; 11 de 11 tablas con checksum IGUAL; reintento sin `-Forzar` rechazado | Pasa |
| T01 | Transacción del llamador con `sp_generar_pago` | `START TRANSACTION; UPDATE; CALL sp_generar_pago; ROLLBACK` | (Comportamiento por documentar) | El `UPDATE` quedó guardado: COMMIT implícito (INC-04). Se documentó como regla para la app | Pasa (documentado) |

## 11.3 Reservadas para el equipo de la aplicación

| ID | Requisito | Qué probar | Observado | Estado |
| -- | --------- | ---------- | --------- | ------ |
| A01 | RF14 Inicio de sesión | Usuario activo entra; inactivo o con contraseña incorrecta no; la contraseña se guarda con hash | — | Pendiente — equipo de la aplicación |
| A02 | RF02 Formulario de empleado | Alta, edición y baja lógica desde la página; sueldo **semanal** > 0; puesto desde la tabla | — | Pendiente — equipo de la aplicación |
| A03 | RF01 Checador en la página | Clave correcta registra la siguiente checada; muestra el mensaje 1644 en domingo o en la 5.ª checada | — | Pendiente — equipo de la aplicación |
| A04 | RF06 Pantalla del ticket | Muestra resumen y renglones de `sp_ticket` igual que la BD | — | Pendiente — equipo de la aplicación |
| A05 | RF07 Cerrar pago | Pide confirmación; después la semana queda en solo lectura | — | Pendiente — equipo de la aplicación |
| A06 | RF11 Aviso de días sin checada | Muestra `v_dias_sin_checada` antes de cerrar | — | Pendiente — equipo de la aplicación |
| A07 | Seguridad de conexión | La app usa `app_floreria` desde user-secrets; ninguna contraseña en el repositorio | — | Pendiente — equipo de la aplicación |
| A08 | Mensajes de error | 1062, 1451, 1452 y 3819 se traducen a mensajes claros | — | Pendiente — equipo de la aplicación |

## 11.4 Capturas que debe tomar el DBA (MySQL Workbench)

Guardar en `Pruebas/capturas/` con estos nombres:

| Archivo sugerido | Qué debe verse |
| ---------------- | -------------- |
| `01_schemas_belissimo.png` | Panel SCHEMAS con `belissimo` desplegado: Tables (11), Views (3), Stored Procedures (5), Functions (3) |
| `02_regresion_ok.png` | Resultado de `Pruebas/verificacion_regresion.sql` con las 22 filas en OK |
| `03_p04_check_sueldo.png` | Output con el error 3819 de P04 |
| `04_p05_fk_restrict.png` | Output con el error 1451 de P05 |
| `05_p07_cuatro_checadas.png` | Resultado de P07 (fila de Carla con las 4 horas) |
| `06_p10_ticket_juan.png` | Los dos resultados de `sp_ticket(2, 1)` |
| `07_p12_rollback.png` | P12: sueldos antes y después del ROLLBACK |
| `08_p13_auditoria.png` | Fila de `auditoria_registro` con la hora anterior y la nueva |
| `09_p14e_pago_cerrado.png` | Error 1644 al cambiar el salario de un pago cerrado |
| `10_p18a_ausencia_cerrada.png` | Error 1644 al cambiar una ausencia de semana cerrada |
| `11_p19_dias_sin_checada.png` | Vista con 29 días y luego 0 después del ROLLBACK |
| `12_p20_rollback_automatico.png` | Error 1452 y el pago 8 sin cambios |
| `13_p16_persistencia.png` | `v_resumen_pago_semanal` después de cerrar y volver a abrir la conexión |
| `14_usuario_app_grants.png` | `SHOW GRANTS FOR 'app_floreria'@'localhost';` |
| `15_diagrama_workbench.png` | Diagrama EER del `.mwb` regenerado (ver `Diagramas/README.md`) |

<div class="salto"></div>

# 12. Incidencias

Solo se registran problemas que **ocurrieron de verdad** al ejecutar los scripts.
Entorno: MySQL Community Server **8.0.46** en Windows 11, cliente `mysql` 8.0, usuario `root`,
`log_bin = 1`, `log_bin_trust_function_creators = 0`, `sql_mode` por omisión de MySQL 8
(`ONLY_FULL_GROUP_BY, STRICT_TRANS_TABLES, NO_ZERO_IN_DATE, NO_ZERO_DATE, ERROR_FOR_DIVISION_BY_ZERO, NO_ENGINE_SUBSTITUTION`).

Antecedente: los scripts se escribieron y probaron primero en MariaDB 10.11. La primera
ejecución en MySQL 8 real fue el 2026-11-12 (base `belissimo`): 01–04 terminaron sin errores.

| ID | Fecha | Dónde | Qué pasó (mensaje exacto) | Causa | Corrección | Verificación |
| -- | ----- | ----- | ------------------------- | ----- | ---------- | ------------ |
| INC-01 | 2026-11-16 | `01_esquema.sql`, `CREATE TABLE` de `empleado`, `usuario` y `pago_semanal` | Cada uno respondió `Query OK, 0 rows affected, 1 warning`. `SHOW WARNINGS`: `Warning 1681 Integer display width is deprecated and will be removed in a future release.` | MySQL 8.0.17+ marca como obsoleto el ancho de despliegue `TINYINT(1)`; MariaDB no lo avisa. | Las columnas `activo` (2) y `cerrado` se declaran `BOOLEAN` (sinónimo que MySQL guarda como `tinyint(1)` sin advertencia). | Reconstrucción completa sin advertencias; regresión 22/22 OK. |
| INC-02 | 2026-11-16 | Trigger `trg_pago_bu_cerrado` | Con el pago 1 **cerrado**, `UPDATE pago_semanal SET min_retardo = 999, id_empleado = 5 WHERE id_pago = 1;` **se aceptó**: el pago cerrado de María quedó a nombre de Carla con 999 min de retardo. | El trigger solo comparaba 5 columnas (`sueldo_base_usado`, `valor_hora`, `descuento_total`, `monto_extra`, `salario_final`) y `cerrado`. | El trigger compara todas las columnas salvo `observaciones`. | Mismo `UPDATE` → `ERROR 1644 (45000): El pago esta cerrado: solo se pueden agregar observaciones.` |
| INC-03 | 2026-11-16 | Tabla `pago_semanal` (sin trigger de borrado) | Con el pago 3 (Ana, semana 1, sin renglones de ticket) **cerrado**, `DELETE FROM pago_semanal WHERE id_pago = 3;` **borró 1 fila**. | `fk_detalle_pago` (ON DELETE RESTRICT) solo protege pagos que tienen renglones; un pago perfecto no tiene. | Nuevo trigger `trg_pago_bd_cerrado`. | Mismo `DELETE` → `ERROR 1644 (45000): El pago esta cerrado: no se puede eliminar.` |
| INC-04 | 2026-11-16 | `sp_generar_pago` llamado dentro de otra transacción | `START TRANSACTION; UPDATE empleado SET telefono='111' WHERE id_empleado=4; CALL sp_generar_pago(4,2,2); ROLLBACK;` → el teléfono **quedó en '111'**. Sin el `CALL`, el mismo `ROLLBACK` sí deshace el cambio. | El procedimiento ejecuta su propio `START TRANSACTION`, y en MySQL eso hace un **COMMIT implícito** de la transacción abierta por quien llama. | No se cambia el SQL: MySQL no tiene una variable confiable para detectarlo (`SELECT @@in_transaction` → `ERROR 1193 (HY000): Unknown system variable 'in_transaction'`). Se documenta como **regla obligatoria** en `Sql/CONTRATO_BD.md`: la app llama `sp_generar_pago` en modo autocommit, nunca dentro de su propia transacción. | Reproducido dos veces en `belissimo_prueba`. |
| INC-05 | 2026-11-16 | Trigger `trg_registro_au_auditoria` | Conectado como el usuario de la app (`app_floreria_prueba`), una corrección de checada dejó en `auditoria_registro` el valor `usuario_bd = root@localhost`. | Dentro de un trigger, `CURRENT_USER()` devuelve la cuenta **DEFINER** del trigger (root), no la cuenta que se conectó. | Se usa `USER()`, que devuelve la cuenta de la sesión. | Misma corrección → `usuario_bd = app_floreria_prueba@localhost`; 17/17 casos de privilegios OK. |

## 12.1 Hallazgos que dependían de una decisión del negocio

| ID | Fecha | Hallazgo (observado) | Decisión y estado |
| -- | ----- | -------------------- | ----------------- |
| PEN-01 | 2026-11-16 | Un empleado puede tener **checada y ausencia el mismo día**. Ana checó a las 08:55 el 2026-10-26 y aun así se aceptó `INSERT INTO ausencia (3, '2026-10-26', 'Injustificada')`; al regenerar su pago se le descontaron $531.25. | **Resuelto el 2026-11-16.** Decisión: un día es checada **o** ausencia, nunca las dos. Se agregaron `trg_registro_bi/bu_ausencia` y `trg_ausencia_bi/bu_checada` (16 triggers en total). Verificado con P21–P21d: el mismo `INSERT INTO ausencia` sobre un día con checadas da `ERROR 1644 (45000): El empleado ya tiene checadas ese dia: no se le puede registrar una ausencia.` |

## 12.2 Observaciones que NO fueron errores

- **Error 1419 (permisos de funciones/triggers):** no ocurrió. Con `log_bin = 1` y
  `log_bin_trust_function_creators = 0`, `root` creó las 3 funciones y los triggers sin cambiar
  la variable. Solo haría falta con un usuario sin privilegio `SUPER`/`SET_USER_ID`.
- Al probar INC-02, el primer intento (`id_empleado = 4`) falló con
  `ERROR 1062 (23000): Duplicate entry '4-1' for key 'pago_semanal.uq_pago_empleado_semana'`.
  Eso fue la restricción `UNIQUE` funcionando bien, no el trigger.

<div class="salto"></div>

# 13. Aplicación web

La aplicación web **Bellissimo** fue desarrollada en **ASP.NET Core Razor Pages (.NET 10)** con **C#** y **Bootstrap** como interfaz gráfica operativa para la administración de recursos humanos, registro de asistencia mediante checador por clave personal, gestión de ausencias, corrección de checadas y cálculo de nómina semanal de la florería.

---

## 13.1 Arquitectura de la aplicación

La solución implementa una arquitectura en **cuatro capas** con flujo unidireccional de dependencias (`Web → Negocios → Datos → MySQL`), separando las responsabilidades de presentación, reglas de negocio, acceso a datos y modelos de dominio:

| Proyecto / Capa | Responsabilidad técnica |
| --- | --- |
| `Bellissimo.Web` | Capa de presentación desarrollada con **Razor Pages** (`.cshtml` y PageModels `.cshtml.cs`) y **Bootstrap**. Gestiona el enrutamiento, el enlace de modelos (*Model Binding*), validaciones de entrada en cliente/servidor y la retroalimentación visual al usuario. |
| `Bellissimo.Negocios` | Capa de lógica de negocio (`PersonalNegocio`, `PuestoNegocio`, `ChecadorNegocio`, `AsistenciaNegocio`, `AusenciaNegocio`, `PagoNegocio`, `SemanaNegocio`, `UsuarioNegocio`). Aplica reglas de negocio, valida precondiciones antes de persistir y traduce excepciones técnicas mediante `ErroresBaseDatos` y `ReglaNegocioException`. |
| `Bellissimo.Datos` | Capa de acceso a datos (`Conexion`, `PersonalDatos`, `PuestoDatos`, `ChecadorDatos`, `RegistroAsistenciaDatos`, `AusenciaDatos`, `PagoDatos`, `SemanaDatos`, `UsuarioDatos`). Ejecuta consultas SQL parametrizadas y llamadas a procedimientos almacenados mediante **ADO.NET (`MySql.Data`)**, atrapando errores de integridad en `BaseDatosException`. |
| `Bellissimo.Entidades` | Biblioteca de clases con los objetos de transferencia y modelos del dominio compartidos entre capas (`Personal`, `Puesto`, `Checada`, `RegistroAsistencia`, `Ausencia`, `Pago`, `Ticket`, `Usuario`). |

---

## 13.2 Conexión y seguridad con MySQL

Para proteger las credenciales de acceso y cumplir con el principio de menor privilegio:

1. **Usuario dedicado del SGBD:** La aplicación nunca utiliza `root`. Se conecta exclusivamente mediante el usuario `app_floreria` (creado en `Sql/05_usuario_app.sql`), el cual cuenta únicamente con permisos de ejecución (`EXECUTE`) sobre rutinas almacenadas y operaciones DML (`SELECT`, `INSERT`, `UPDATE`, `DELETE`) sobre las tablas operativas de la base de datos `belissimo`, sin privilegios administrativos ni de alteración de esquema (`DDL`).
2. **Gestión de credenciales fuera del código fuente:** La cadena de conexión (`ConnectionStrings:Bellissimo`) no se almacena en `appsettings.json` ni en el repositorio de Git. En su lugar, se configura localmente mediante **User Secrets** de .NET (`dotnet user-secrets set`), siguiendo la plantilla documentada en `Sql/conexion_plantilla.md`:

```json
{
  "ConnectionStrings": {
    "Bellissimo": "Server=localhost;Port=3306;Database=belissimo;User ID=app_floreria;Password=<contraseña>;CharSet=utf8mb4;SslMode=Preferred"
  }
}
```

3. **Consultas parametrizadas:** Toda comunicación desde `Bellissimo.Datos` utiliza parámetros tipados (`MySqlParameter`) o procedimientos almacenados, mitigando ataques de inyección SQL.

---

## 13.3 Módulos y capturas de la aplicación funcionando

### 13.3.1 Navegación general e inicio
La pantalla principal (`Pages/Index.cshtml`) centraliza el acceso mediante una barra de navegación superior hacia los módulos operativos: **Inicio**, **Entradas**, **Salidas**, **Asistencias**, **Ausencias**, **Pagos**, **Personal** y **Puestos**.

![Pantalla principal y navegación del sistema](capturas/01_inicio.png)

### 13.3.2 Catálogo de Puestos (CRUD — Entidad 1)
El módulo `Pages/Puestos/` permite consultar los puestos laborales vigentes, registrar nuevos cargos con su sueldo base y tarifa por hora, así como editar sus montos.

![Gestión y catálogo de puestos](capturas/02_puestos_crud.png)

### 13.3.3 Gestión de Personal (CRUD y búsqueda — Entidad 2)
El módulo `Pages/Personal/` implementa el ciclo completo de administración de empleados: listado general con filtro de búsqueda por nombre o clave, consulta de detalle, alta de nuevos trabajadores asociados a una llave foránea de `Puesto` y edición con baja lógica.

![Listado y búsqueda de personal](capturas/03_personal_lista.png)

![Alta y detalle de empleado con asignación de puesto](capturas/04_personal_alta.png)

### 13.3.4 Gestión de Ausencias (CRUD — Entidad 3)
En `Pages/Ausencias/` se registran, editan y eliminan las faltas o permisos asociados a un empleado (`Personal`), indicando fecha, tipo de ausencia y justificación para su consideración en el cálculo semanal.

![Registro y administración de ausencias](capturas/05_ausencias_crud.png)

### 13.3.5 Checador de Entradas y Salidas con clave personal
Los módulos `Pages/RegistroEntradas/` y `Pages/RegistroSalidas/` operan como terminal de checado rápido mediante la clave personal del trabajador, registrando entrada laboral, salida a comer, regreso de comida y salida final del turno.

![Registro de entrada mediante clave personal](capturas/06_checador_entrada.png)

### 13.3.6 Generación de Semanas, Nómina y Ticket de Pago (Operación multitabla)
El módulo `Pages/Pagos/` ejecuta la operación de negocio principal del sistema: a partir de una semana laboral (`Semana`), consolida información de múltiples tablas (`Personal`, `Puesto`, `RegistroAsistencia` y `Ausencia`), aplica reglas de cálculo y genera los registros en `Pago` junto con su desglose individual en `Pages/Pagos/Ticket.cshtml`.

![Generación y listado de pagos semanales](capturas/07_pagos_semana.png)

![Ticket de pago semanal con desglose de conceptos y cierre](capturas/08_pagos_ticket.png)

### 13.3.7 Reporte de Asistencias, correcciones y manejo de estados vacíos
El módulo `Pages/Asistencias/` permite realizar correcciones manuales autorizadas sobre registros de asistencia y consultar el reporte consolidado cruzando empleado, puesto, horarios y observaciones (`Pages/Asistencias/Reporte.cshtml`), así como visualizar días sin checada. Cuando un filtro no arroja coincidencias, la interfaz presenta un estado vacío controlado sin errores.

![Reporte multitabla de asistencias con filtros](capturas/09_asistencias_reporte.png)

![Manejo de estado vacío ante búsquedas sin coincidencias](capturas/10_estado_vacio.png)

---

## 13.4 Validaciones, control de errores y evidencia de persistencia

### 13.4.1 Validación de campos y reglas de negocio
La aplicación valida datos tanto en el formulario (campos requeridos, tipos numéricos, rangos de fecha y formatos) como en la capa de negocio y el SGBD (claves inexistentes, registros duplicados o restricciones de integridad referencial), mostrando mensajes claros al usuario sin exponer trazas técnicas.

![Validación de campos obligatorios y formato](capturas/11_validacion_campos.png)

![Control de error ante violación de regla de negocio o integridad](capturas/12_error_regla_negocio.png)

### 13.4.2 Acreditación de persistencia real en MySQL
Para comprobar que toda operación impacta directamente en el SGBD relacional sin almacenamiento simulado en memoria:
1. Se realizó un registro desde la interfaz web y se verificó su almacenamiento físico ejecutando una consulta `SELECT` directamente en **MySQL Workbench**.
2. Se detuvo por completo el servidor local de la aplicación (`Bellissimo.Web`), se cerró el navegador y se volvió a iniciar la ejecución (`dotnet run`), confirmando que los registros persisten y se recuperan íntegramente desde MySQL.

![Verificación directa del registro en MySQL Workbench](capturas/13_persistencia_workbench.png)

![Persistencia comprobada tras reiniciar la aplicación web](capturas/14_persistencia_reapertura.png)

---

## 13.5 Pruebas de interfaz (A01–A08)

A continuación se documentan los resultados observados al ejecutar los casos de prueba funcionales de interfaz definidos en la matriz de pruebas (Sección 11):

| ID | Caso de prueba | Módulo / Ruta | Datos / Acción ejecutada | Resultado observado | Estado |
| --- | --- | --- | --- | --- | :---: |
| **A01** | Alta de empleado con puesto válido | `/Personal/Nuevo` | Captura de datos completos de empleado nuevo vinculado a un `id_puesto` existente y envío de formulario. | Se inserta el registro en la tabla `personal`, muestra mensaje de confirmación y aparece en el listado general. | **Aprobado** |
| **A02** | Validación de campos obligatorios y formato | `/Personal/Nuevo` y `/Puestos/Nuevo` | Envío de formulario con campos vacíos o valores numéricos fuera de rango. | La interfaz bloquea el envío y despliega alertas de validación junto a cada campo inválido sin fallar. | **Aprobado** |
| **A03** | Búsqueda y filtro de empleados (con resultados y estado vacío) | `/Personal/Index` | Búsqueda por nombre existente y posterior búsqueda con cadena inexistente (`"ZZZZ"`). | Filtra correctamente las coincidencias en el primer caso y muestra mensaje controlado de estado vacío en el segundo. | **Aprobado** |
| **A04** | Edición y baja lógica de personal | `/Personal/Editar` | Modificación de datos del empleado y cambio de estatus activo a inactivo. | Actualiza el registro en MySQL preservando su historial de asistencias y pagos sin romper integridad referencial. | **Aprobado** |
| **A05** | Registro de checada con clave personal válida e inválida | `/RegistroEntradas/RegistroEntrada` | Ingreso de clave personal registrada y posterior prueba con clave inexistente (`"999999"`). | Con clave válida registra la hora exacta en `registro_asistencia`; con clave inválida muestra error controlado de negocio. | **Aprobado** |
| **A06** | Registro y consulta de ausencia (CRUD) | `/Ausencias/Nuevo` | Alta de ausencia asociada a empleado y fecha específica. | Persiste en `ausencia`, se visualiza en el listado y se refleja en el cálculo de descuentos de la semana correspondiente. | **Aprobado** |
| **A07** | Generación de semana, cálculo de pago y ticket | `/Pagos/Index` y `/Pagos/Ticket` | Generación de nómina semanal, consulta de ticket individual y agregado de nota/observación. | Consolida sueldos, descuentos y extras en `pago`, despliega el ticket con su desglose y guarda las observaciones. | **Aprobado** |
| **A08** | Reporte de asistencia multitabla y persistencia tras reinicio | `/Asistencias/Reporte` | Consulta filtrada por fechas/empleado, cierre del servidor web, reapertura y consulta en MySQL Workbench. | Muestra datos cruzados de personal, puesto y checadas; tras reiniciar la app y consultar el SGBD, todos los datos permanecen intactos. | **Aprobado** |

---

## 13.6 Cumplimiento del contrato de BD y validaciones de interfaz

La implementación de la capa web y de negocios respeta estrictamente las reglas establecidas en `Sql/CONTRATO_BD.md` y `Sql/validaciones_interfaz.md`:

* **Regla 0 del contrato (`CONTRATO_BD.md`):** Ninguna regla crítica de integridad reside únicamente en el cliente. Las restricciones de llaves primarias, llaves foráneas, unicidad (`UNIQUE`), obligatoriedad (`NOT NULL`), dominios (`CHECK`) y lógica transaccional se hacen cumplir en el motor **MySQL 8.0** (mediante restricciones de esquema, procedimientos almacenados y triggers), mientras que `Bellissimo.Negocios` y `Bellissimo.Web` replican las validaciones preventivas para mejorar la experiencia de usuario.
* **No duplicidad de efectos con triggers:** En operaciones donde un trigger del SGBD actualiza automáticamente campos derivados o de auditoría (como cálculos asociados a registros de asistencia o cierres), la capa `Bellissimo.Datos` delega dicha actualización al motor de base de datos sin ejecutar una segunda sentencia `UPDATE` redundante desde C#.
* **Manejo controlado de excepciones SQL:** Cuando MySQL rechaza una operación por violación de restricción (`SQLSTATE` de llave duplicada, referencia inexistente o regla de negocio lanzada con `SIGNAL SQLSTATE '45000'`), `BaseDatosException` y `ErroresBaseDatos` capturan el código de error y presentan un mensaje comprensible en pantalla.

<div class="salto"></div>

# 14. Conclusiones

## 14.1 Conclusión del equipo

El desarrollo de **Bellissimo** permitió integrar de forma práctica el diseño de una base de datos relacional normalizada en **MySQL 8.0** con una aplicación web funcional en **ASP.NET Core Razor Pages (.NET 10)**. A lo largo del proyecto comprobamos que delegar las reglas críticas de integridad, transacciones y cálculos automatizados directamente al SGBD —respaldado por un contrato técnico claro entre capas— garantiza la consistencia y persistencia real de la información operativa de la florería (asistencias, ausencias y pagos semanales), evitando depender únicamente de validaciones en la interfaz.

## 14.2 Conclusión individual — DBA (Jose Antonio Montoya Garcia)

**Decisiones que tomé y por qué**
- Se decidió almacenar en la tabla de `pago` el historial calculado ("foto" de sueldos, descuentos y conceptos al momento de generar y cerrar la semana) para preservar la integridad histórica de los recibos aunque posteriormente cambie el sueldo base de un puesto.
- Se implementaron triggers, procedimientos almacenados y restricciones (`FOREIGN KEY`, `UNIQUE`, `CHECK`) directamente en MySQL 8.0 en lugar de depender solo de la aplicación, junto con un usuario dedicado de mínimo privilegio (`app_floreria`) para proteger el esquema.

**Dificultades**
- Ajustar la sintaxis y el comportamiento estricto al probar sobre MySQL 8.0 real, así como documentar y resolver las incidencias detectadas durante las pruebas de regresión (como el manejo de días sin checada, traslapes de ausencias y bloqueo de cambios en semanas cerradas).
- Formalizar en reglas de negocio verificables procesos cotidianos de la florería que inicialmente se manejaban de manera informal.

**Mi contribución al proyecto**
- Levantamiento de requerimientos del negocio, diseño conceptual y relacional (diagramas, diccionario de datos y normalización), codificación de los scripts SQL (`01_esquema.sql` a `05_usuario_app.sql`), definición del contrato técnico (`CONTRATO_BD.md`) y ejecución de la matriz de pruebas y registro de incidencias.

**Lo que aprendí**
- Aplicar la teoría de normalización (1FN a 3FN y análisis de formas superiores) frente a necesidades reales de auditoría histórica, programar transacciones y triggers robustos en MySQL y coordinar entregas versionadas en equipo mediante Git.

## 14.3 Conclusiones individuales del resto del equipo

### Kenan Rojas Arroyo (Desarrollo Frontend y Experiencia de Usuario)

**Decisiones que tomé y por qué**
- Se diseñó la capa de presentación (`Bellissimo.Web`) con Razor Pages y Bootstrap organizando cada módulo en vistas dedicadas (`Index`, `Nuevo`, `Editar`, `Detalle`, `Eliminar`) con enlace de modelos tipado y validaciones alineadas a `Sql/validaciones_interfaz.md`.

**Dificultades**
- Asegurar que los errores de integridad y reglas de negocio devueltos por MySQL (claves inexistentes en el checador, registros duplicados o semanas cerradas) se mostraran como alertas claras para el usuario sin exponer excepciones técnicas, además de controlar los estados vacíos en reportes y tickets sin incidencias.

**Mi contribución al proyecto**
- Desarrollo de las interfaces web, formularios, navegación, reportes y tickets de pago en `Bellissimo.Web`, así como la ejecución y documentación de las pruebas funcionales de interfaz.

**Lo que aprendí**
- Construir una interfaz web conectada a una arquitectura en capas que respeta la integridad del SGBD y ofrece retroalimentación clara sobre operaciones válidas, inválidas y persistencia de datos.

### Troy Leonardo Moreno Calderón (Arquitectura Backend, Lógica de Negocio y Liderazgo)

**Decisiones que tomé y por qué**
- Se estructuró la solución en cuatro capas (`Web → Negocios → Datos → MySQL`) utilizando ADO.NET con consultas parametrizadas, gestión de credenciales mediante *User Secrets* y una jerarquía de excepciones (`BaseDatosException`, `ErroresBaseDatos`, `ReglaNegocioException`) para traducir códigos `SQLSTATE` hacia la interfaz.

**Dificultades**
- Coordinar la sincronización entre los procedimientos almacenados de MySQL y las llamadas desde `Bellissimo.Datos` para evitar duplicar cálculos o disparos de triggers durante la generación de pagos y el registro de checadas, así como integrar las distintas ramas de trabajo (`backend`, `frontend`, `dba`) en `main` sin conflictos.

**Mi contribución al proyecto**
- Diseño de la arquitectura de software en C#, implementación de las capas `Bellissimo.Entidades`, `Bellissimo.Datos` y `Bellissimo.Negocios`, integración del flujo de nómina semanal, documentación de pruebas de backend (`PRUEBAS_BACKEND.md`) y coordinación general del repositorio.

**Lo que aprendí**
- Desacoplar la lógica de negocio del acceso a datos usando ADO.NET puro, respetar contratos estrictos entre la aplicación y el motor de base de datos, y liderar el flujo de integración continua del equipo con Git y GitHub.

<div class="salto"></div>

# 15. Referencias

## 15.1 Libros y artículos

- Chen, P. P. (1976). The entity-relationship model—toward a unified view of data. *ACM
  Transactions on Database Systems, 1*(1), 9–36.
- Codd, E. F. (1970). A relational model of data for large shared data banks. *Communications of
  the ACM, 13*(6), 377–387.
- Date, C. J. (2004). *An introduction to database systems* (8.ª ed.). Addison-Wesley.
- Elmasri, R. y Navathe, S. B. (2016). *Fundamentals of database systems* (7.ª ed.). Pearson.
- Fagin, R. (1977). Multivalued dependencies and a new normal form for relational databases.
  *ACM Transactions on Database Systems, 2*(3), 262–278.
- Kent, W. (1983). A simple guide to five normal forms in relational database theory.
  *Communications of the ACM, 26*(2), 120–125.
- National Institute of Standards and Technology. (1993). *Integration definition for information
  modeling (IDEF1X)* (FIPS PUB 184).
- Silberschatz, A., Korth, H. F. y Sudarshan, S. (2020). *Database system concepts* (7.ª ed.).
  McGraw-Hill Education.

## 15.2 Documentación técnica en línea

Consultadas el 17 de noviembre de 2026.

- Oracle. *MySQL 8.0 Reference Manual*. https://dev.mysql.com/doc/refman/8.0/en/
  - 15.1.22 CREATE TRIGGER Statement. https://dev.mysql.com/doc/refman/8.0/en/create-trigger.html
  - 15.3.1 START TRANSACTION, COMMIT, and ROLLBACK Statements.
    https://dev.mysql.com/doc/refman/8.0/en/commit.html
  - 15.3.3 Statements That Cause an Implicit Commit (incidencia INC-04).
    https://dev.mysql.com/doc/refman/8.0/en/implicit-commit.html
  - 15.1.20.6 CHECK Constraints. https://dev.mysql.com/doc/refman/8.0/en/create-table-check-constraints.html
  - 13.1.6 Numeric Type Attributes (ancho de despliegue obsoleto desde 8.0.17, incidencia INC-01).
    https://dev.mysql.com/doc/refman/8.0/en/numeric-type-attributes.html
  - 14.15 Information Functions (`USER()` y `CURRENT_USER()`, incidencia INC-05).
    https://dev.mysql.com/doc/refman/8.0/en/information-functions.html
  - 27.6 Stored Object Access Control (permisos `DEFINER` de rutinas, vistas y triggers).
    https://dev.mysql.com/doc/refman/8.0/en/stored-objects-security.html
  - 15.2.8 INTERSECT Clause y 15.2.4 EXCEPT Clause (desde 8.0.31).
    https://dev.mysql.com/doc/refman/8.0/en/intersect.html ·
    https://dev.mysql.com/doc/refman/8.0/en/except.html
  - 6.5.4 mysqldump — A Database Backup Program. https://dev.mysql.com/doc/refman/8.0/en/mysqldump.html
- Oracle. *MySQL Connector/NET Developer Guide*. https://dev.mysql.com/doc/connector-net/en/
- Microsoft. *Safe storage of app secrets in development in ASP.NET Core*. Microsoft Learn.
  https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets

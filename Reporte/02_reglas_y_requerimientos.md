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

**Estado de la aplicación:** la interfaz web está en desarrollo por el equipo de la aplicación;
las pantallas, la autenticación y su despliegue son *[a cargo de frontend/backend]*.

**Datos:** todos los datos de prueba son **ficticios** (5 empleados, 2 semanas). No se usan datos
reales de los empleados.

## 2.3 Actores y usuarios

| Actor | Tipo | Cómo interactúa con el sistema | Cuenta / acceso |
| ----- | ---- | ------------------------------ | --------------- |
| **Dueño** | Usuario de la página | Configura parámetros del cálculo y el horario; consulta pagos, reportes y auditoría; *supuesto:* corrige checadas olvidadas | `usuario` con rol `Dueno` |
| **Encargado** | Usuario de la página | Registra ausencias, corrige checadas (queda auditado), genera, revisa y cierra pagos, agrega observaciones, consulta el ticket | `usuario` con rol `Encargado` |
| **Empleado** | Actor del checador | Registra sus 4 checadas diarias con su clave personal (huella en el futuro); no inicia sesión en la página | Fila en `empleado` (`clave_hash`) |
| **Aplicación web** | Sistema | Ejecuta procedimientos y consultas en nombre de los usuarios | Cuenta MySQL `app_floreria` (mínimo privilegio) |
| **DBA** | Administrador | Crea y mantiene la base, respaldos y pruebas | Cuenta administrativa de MySQL (Workbench) |
| **Contador** | Interesado externo | No usa el sistema; debe confirmar los multiplicadores de tiempo extra | — |

Qué pantallas ve cada rol en la página: *[a confirmar con el equipo de la app]*.

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
| RN16 | Contraseñas y claves personales se guardan solo como hash. | Convención: `usuario.contrasena` y `empleado.clave_hash` `VARCHAR(255)`; **lo hace la app** (la BD no puede verificar que sea hash). | Revisión de la app (pendiente — equipo de la aplicación) |
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

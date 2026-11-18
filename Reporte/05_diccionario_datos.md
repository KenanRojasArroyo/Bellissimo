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

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

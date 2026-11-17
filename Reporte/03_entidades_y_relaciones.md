# 3. Entidades, claves y relaciones

Base `belissimo` (MySQL 8.0). Todo lo que se dice de restricciones y llaves se puede comprobar en
la sección 5 (diccionario de datos), que se generó desde `information_schema`.

## 3.1 Entidades

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
| `detalle_pago` | **Débil** (por existencia) | Un renglón del ticket solo existe dentro de un pago. No tiene llave natural declarada (ver §3.2). |
| `auditoria_registro` | **Fuerte** (bitácora) | A propósito **no** tiene FK: debe conservarse aunque el registro auditado se borre. |

> **Nota de implementación.** Todas las tablas débiles usan una PK sustituta `AUTO_INCREMENT`, y su
> llave natural queda como `UNIQUE`. Por eso en el modelo físico todas las relaciones son **no
> identificadoras**: la FK no forma parte de la PK. Así aparece en el diagrama IDEF1X, con líneas
> punteadas. Se eligió así porque la app y las FK trabajan con un solo entero, y porque la llave
> natural sigue protegida.

## 3.2 Claves candidatas

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

## 3.3 Relaciones: cardinalidad y participación

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

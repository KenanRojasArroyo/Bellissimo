# Modelo de datos y normalización

Base `belissimo` (MySQL 8.0). Todo lo que se dice de restricciones y llaves se puede comprobar en
`Reporte/diccionario_datos.md`, que se generó desde `information_schema`.

## 1. Entidades

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
| `detalle_pago` | **Débil** (por existencia) | Un renglón del ticket solo existe dentro de un pago. No tiene llave natural declarada (ver §2). |
| `auditoria_registro` | **Fuerte** (bitácora) | A propósito **no** tiene FK: debe conservarse aunque el registro auditado se borre. |

> **Nota de implementación.** Todas las tablas débiles usan una PK sustituta `AUTO_INCREMENT`, y su
> llave natural queda como `UNIQUE`. Por eso en el modelo físico todas las relaciones son **no
> identificadoras**: la FK no forma parte de la PK. Así aparece en el diagrama IDEF1X, con líneas
> punteadas. Se eligió así porque la app y las FK trabajan con un solo entero, y porque la llave
> natural sigue protegida.

## 2. Claves candidatas

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

## 3. Relaciones: cardinalidad y participación

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

## 4. Normalización

### 4.1 Esquema inicial: la hoja que hoy se lleva a mano

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

### 4.2 Primera forma normal (1FN)

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

### 4.3 Segunda forma normal (2FN)

Hay **dependencias parciales**: atributos que dependen solo de una parte de la PK
(`id_empleado` o `fecha`). Se separan:

```text
EMPLEADO( id_empleado, nombre, telefono, id_puesto, puesto, sueldo_base )
DIA( fecha, semana_inicio, semana_fin, dia_semana, hora_entrada_oficial, hora_salida_oficial )
REGISTRO( id_empleado, fecha, entrada, salida_comida, regreso_comida, salida, tipo_ausencia )
PAGO( id_empleado, semana_inicio, total_descuentos, total_extra, salario_final )
```

### 4.4 Tercera forma normal (3FN)

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
  `pago_semanal` como **desnormalización controlada** (§4.6).
- Los valores que el dueño puede cambiar (48 h, 5 min, 30 min, 540 min, ×2, ×3) pasan a
  **`configuracion`**, en lugar de repetirse en cada cálculo.
- Se agregan `usuario` (quién usa la página) y `auditoria_registro` (quién corrigió qué).

**Resultado:** el esquema final de 11 tablas en `Sql/01_esquema.sql`. No hubo que hacerle cambios
de normalización: **ya cumple 3FN** salvo las desnormalizaciones controladas que se justifican
abajo. No se inventaron errores en el diseño final.

### 4.5 Revisión del esquema final: BCNF, 4FN y 5FN

**BCNF** (todo determinante de una DF no trivial es superllave). Se revisó tabla por tabla:

| Tabla | ¿BCNF? | Comentario |
| ----- | ------ | ---------- |
| `puesto`, `usuario`, `horario_general`, `configuracion`, `ausencia` | Sí | Sus únicos determinantes son la PK o una clave candidata `UNIQUE`. |
| `empleado` | Sí | `id_huella` determina todo, pero es clave candidata (`UNIQUE`). |
| `semana` | Sí, si se reconoce `fecha_fin` como clave candidata | `fecha_inicio → fecha_fin` y `fecha_fin → fecha_inicio`. Ambas son claves; solo `fecha_inicio` está declarada `UNIQUE`. `fecha_fin` además es derivable (`fecha_inicio + 5 días`) y se guarda para que las consultas `BETWEEN` sean simples. |
| `registro_asistencia` | **No estricta** | Por `ck_registro_fecha` existe la DF `entrada → fecha` (`DATE(entrada) = fecha`), y `entrada` no es superllave (dos empleados pueden entrar el mismo minuto). Como `fecha` es **atributo primo** (forma parte de `id_empleado, fecha`), la tabla **sí cumple 3FN** pero no BCNF. Se acepta porque `fecha` hace falta como llave aunque `entrada` sea NULL (correcciones manuales), y el `CHECK` impide la anomalía. |
| `pago_semanal` | **No** (desnormalización controlada) | `sueldo_base_usado → valor_hora` es transitiva, y los totales (`descuento_total`, `monto_extra`, `min_*`, `salario_final`) se derivan de `detalle_pago`. Ver §4.6. |
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

### 4.6 Desnormalización controlada de `pago_semanal` y `detalle_pago`

Los totales y renglones del pago se **guardan** en lugar de calcularse cada vez que se consultan.
Es intencional:

1. **Foto histórica.** El ticket justifica lo que se pagó. Si mañana cambia el sueldo de Juan, la
   tolerancia o el multiplicador del tiempo extra, el pago de la semana 1 debe seguir diciendo
   $2,807.28. Por eso se guardan `sueldo_base_usado` y `valor_hora`, aunque se puedan calcular.
2. **Ticket congelado.** Al cerrar el pago, 12 triggers impiden cambiar checadas, ausencias,
   renglones y montos de esa semana. Que el dato derivado sea inmutable elimina la anomalía de
   actualización.
3. **Una sola fuente que escribe.** Solo `sp_generar_pago` escribe totales y renglones, dentro de
   una transacción; los totales son la suma de los renglones. La app no recalcula.
4. **Rendimiento.** La pantalla principal del encargado (`v_resumen_pago_semanal`) lee un renglón
   por pago, en vez de recalcular minutos de cada checada de la semana.

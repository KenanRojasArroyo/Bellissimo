# Base de datos — Reloj checador y pago semanal (Florería Bellissimo)

**Motor:** MySQL 8.0 (probado en 8.0.46) · **Herramienta:** MySQL Workbench · **Base:** `belissimo`
· **Juego de caracteres:** `utf8mb4` / `utf8mb4_unicode_ci`

## 1. Cómo reconstruir la base

Ejecutar **en este orden** (Workbench: File → Open SQL Script → rayo; o el cliente `mysql`):

| # | Archivo | Qué hace |
| - | ------- | -------- |
| 1 | `Sql/01_esquema.sql` | Elimina y crea la base, 11 tablas, restricciones, índices y catálogos |
| 2 | `Sql/02_datos.sql` | Datos de prueba **ficticios**: 5 empleados, 2 semanas, 44 checadas, 4 ausencias |
| 3 | `Sql/03_rutinas.sql` | 3 funciones, 5 procedimientos y 12 triggers |
| 4 | `Sql/04_consultas_vistas.sql` | Genera los 8 pagos de prueba, 3 vistas y 10 consultas documentadas |
| 5 | `Sql/05_usuario_app.sql` | Usuario `app_floreria` de mínimo privilegio (cambiar la contraseña en la copia local) |
| — | `Pruebas/verificacion_regresion.sql` | Confirma objetos y pagos esperados (todo debe decir `OK`) |
| — | `Pruebas/pruebas_bd.sql` | Casos de prueba válidos e inválidos (cambia datos a propósito) |

Otros archivos de esta carpeta:

| Archivo | Para qué |
| ------- | -------- |
| `CONTRATO_BD.md` | Procedimientos, funciones, vistas, errores y ejemplos C# para el equipo de la app |
| `conexion_plantilla.md` | Cadena de conexión con placeholders y cómo usar user-secrets / variables de entorno |
| `validaciones_interfaz.md` | Reglas que la interfaz también debe validar |
| `respaldo_restauracion.ps1` | Respaldo (`mysqldump` con rutinas y triggers) y restauración |
| `diccionario_datos.sql` | Genera el diccionario desde `information_schema` |
| `algebra_relacional.sql` | SQL equivalente a las expresiones de álgebra relacional del reporte |

Desde PowerShell (el operador `<` no existe ahí; los scripts usan `DELIMITER`, por eso se usa `source`):

```powershell
& "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" -u root -p --default-character-set=utf8mb4 -e "source Sql/01_esquema.sql"
```

> **Cuidado:** `01_esquema.sql` empieza con `DROP DATABASE IF EXISTS belissimo;`. Borra todo lo
> que haya en esa base. Si se corrió `pruebas_bd.sql` y se quiere repetir, reconstruir desde `01`.

**Permisos:** si el binlog está activo, crear funciones y triggers exige `SUPER`. Con `root` no hace
falta nada más (verificado en MySQL 8.0.46). Con otro usuario, si aparece el error 1419, un
administrador ejecuta `SET GLOBAL log_bin_trust_function_creators = 1;`.

**Equivalencia con la numeración original:** `01_esquema`, `02_datos`, `03_rutinas` y
`04_consultas_vistas` conservan su número. El antiguo `05_pruebas.sql` ahora es
`Pruebas/pruebas_bd.sql`. No se reordenó a "03 consultas / 04 rutinas" porque las vistas y la
preparación de pagos dependen de funciones y procedimientos que se crean en `03_rutinas`.

**Estado de verificación:** los scripts corren sin errores ni advertencias en MySQL 8.0.46
(2026-11-16). Incidencias reales encontradas y corregidas: `Pruebas/incidencias.md`.

**Requisitos:** MySQL Server 8.0.31 o posterior (por `INTERSECT`/`EXCEPT` en
`algebra_relacional.sql`; los scripts 01–05 funcionan con cualquier 8.0), usuario con privilegio
para crear bases, rutinas y triggers (root), y Windows PowerShell para el script de respaldo.

### Mapa de las carpetas de base de datos

| Carpeta | Contenido |
| ------- | --------- |
| `Sql/` | Scripts 01–05, contrato para la app, plantilla de conexión, validaciones, respaldo, diccionario y álgebra |
| `Diagramas/` | Crow's Foot, Connect to Columns, UML e IDEF1X (PNG/PDF), fuentes editables y guía para Classic y el `.mwb` en Workbench |
| `Pruebas/` | `pruebas_bd.sql`, `verificacion_regresion.sql`, `matriz_pruebas.md`, `incidencias.md` y `evidencias/` |
| `Reporte/` | Secciones de BD del reporte (`00`–`13`), `Reporte_BD.md` / `.pdf` armados con `herramientas/armar_reporte.py` |
| `Presentacion/`, `Video/` | Apartados de BD de la presentación y guion del video |

## 2. Tablas

| Tabla | Para qué sirve |
| ----- | -------------- |
| `puesto` | Recepcionista, Repartidor, Florista |
| `empleado` | Datos de la persona, puesto y **sueldo base semanal** (se captura al darla de alta) |
| `usuario` | Dueño y encargado que usan la página (contraseña como **hash**) |
| `horario_general` | Entrada y salida por día (lun–vie 9:00–17:30, sáb 9:00–15:30) |
| `configuracion` | Parámetros editables: horas base, tolerancia, comida, reglas de tiempo extra y retardos |
| `registro_asistencia` | Las 4 checadas del día, método (Clave/Huella/Manual) y quién la corrigió |
| `ausencia` | Justificada, Injustificada o Vacaciones |
| `semana` | Periodo de pago (lunes a sábado) |
| `pago_semanal` | Resumen del pago de un empleado en una semana |
| `detalle_pago` | Renglones del ticket, congelados tal como se calcularon |
| `auditoria_registro` | Bitácora de correcciones de checadas (la llena un trigger) |

Diagramas: carpeta `Diagramas/`.

## 3. Cómo se calcula el pago

```text
Valor de la hora = sueldo base ÷ 48
Salario          = sueldo base − descuentos + tiempo extra   (mínimo $0)
```

| Situación | Tratamiento |
| --------- | ----------- |
| Entrada hasta 5 min tarde | Puntual (tolerancia) |
| Entrada después de la tolerancia | Retardo medido desde la hora oficial; se acumula en la semana (informativo, ver pendientes) |
| Comida mayor a 30 min | Los minutos de más se descuentan |
| Salida antes de la hora | Los minutos faltantes se descuentan |
| Falta injustificada | Se descuenta la jornada del día (510 min entre semana, 390 sábado) |
| Falta justificada o vacaciones | No se descuenta |
| Día sin checada y sin ausencia | **No se descuenta solo**: aparece en `v_dias_sin_checada` para que el encargado capture la ausencia |
| Tiempo extra | Primeros 540 min de la semana × 2; lo que pase × 3 (valores en `configuracion`) |

El descuento y el extra del ticket son la **suma de renglones**, cada uno redondeado a centavos.

## 4. Uso desde la página web

```sql
CALL sp_registrar_checada(id_empleado, ahora, 'Clave');   -- siguiente checada del día
CALL sp_generar_pago(id_empleado, id_semana, id_usuario); -- calcula (transacción) y puede regenerarse
CALL sp_ticket(id_empleado, id_semana);                   -- muestra el ticket sin recalcular
CALL sp_agregar_observacion(id_pago, 'texto');            -- permitido aun con el pago cerrado
CALL sp_cerrar_pago(id_pago);                             -- congela pago, ticket, checadas y ausencias de la semana
```

Referencia completa para la aplicación (parámetros, resultados, errores, ejemplos en C#):
`Sql/CONTRATO_BD.md`.

## 5. Triggers (12)

| Trigger | Efecto |
| ------- | ------ |
| `trg_registro_bi/bu/bd_cerrado` | Impiden insertar, modificar o borrar checadas de una semana cerrada |
| `trg_registro_au_auditoria` | Guarda hora anterior y nueva, usuario de la app y usuario de BD al corregir una checada |
| `trg_ausencia_bi/bu/bd_cerrado` | Impiden insertar, modificar o borrar ausencias de una semana cerrada |
| `trg_detalle_bi/bu/bd_cerrado` | Congelan el ticket de un pago cerrado |
| `trg_pago_bu_cerrado` | En un pago cerrado solo permite cambiar `observaciones` |
| `trg_pago_bd_cerrado` | Un pago cerrado no se puede borrar |

## 6. Vistas (3)

| Vista | Uso |
| ----- | --- |
| `v_asistencia_diaria` | Una fila por empleado y día: horas, retardo, comida de más, salida temprana, extra |
| `v_resumen_pago_semanal` | Pago de cada empleado por semana con puesto y estado (Borrador/Cerrado) |
| `v_dias_sin_checada` | Días laborales ya pasados sin checada ni ausencia (revisar antes de cerrar el pago) |

## 7. Resultados esperados con los datos de prueba

| Empleado | Semana 1 | Semana 2 |
| -------- | -------- | -------- |
| María López | $2,911.45 (85 min de comida de más) | $3,000.00 |
| Juan Pérez | $2,807.28 (desc. 630.22, extra 137.50) | $5,156.25 (extra 1,856.25) |
| Ana Torres | $3,000.00 | $2,468.75 (falta 531.25) |
| Luis Ramírez | $3,150.00 | $3,937.50 (extra 787.50) |

Total a pagar: semana 1 = $11,868.73, semana 2 = $14,562.50. Carla Mendoza (ingresó el
2026-11-09) no tiene pagos: sirve para probar `LEFT JOIN` y `NOT EXISTS`.

## 8. Pendientes con el dueño

| Pendiente | Hoy en el sistema |
| --------- | ----------------- |
| ¿Cuántos minutos acumulados de retardo descuentan 1 h y 2 h? | No descuentan. Basta llenar `retardo_min_para_1h` y `retardo_min_para_2h` en `configuracion` |
| ¿La falta justificada se paga completa? | Se **supone que sí** |
| Multiplicadores de tiempo extra | 2× y 3× (estilo Ley Federal del Trabajo); **confirmar con el contador**. No aplica topes legales diarios |
| ¿Quién corrige una checada olvidada? | Se supone dueño o encargado desde la página; queda auditado |
| Bonificaciones y vacaciones por antigüedad | Etapa 2 (fuera de alcance) |
| Checado por huella | Pospuesto; la BD ya tiene `id_huella` y método `Huella` |

## 9. Limitaciones y observaciones

- **48 h base vs 49 h de presencia.** El dueño confirmó que el sueldo se calcula sobre 48 h; por eso
  el pago es *sueldo base − tiempo perdido (+ extra)* y quien cumple todo cobra exactamente su base.
  Con seis faltas completas el descuento rebasa el sueldo y el salario se limita a $0.
- **`pago_semanal` guarda totales derivados** a propósito, como foto histórica del pago (ver la
  sección de normalización del reporte).
- **Días sin checada y sin ausencia** no se descuentan solos; `v_dias_sin_checada` los muestra.
- **Borrar una checada** de una semana abierta no queda en la auditoría (solo se auditan correcciones
  con `UPDATE`). Si el dueño quiere rastrear también borrados, se agrega un trigger `AFTER DELETE`.
- **Contraseñas y claves:** guardar solo hashes. Los de prueba son `HASH_DE_PRUEBA_NO_USAR`.

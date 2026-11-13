# Base de datos — Reloj checador y pago semanal (Florería)

**Motor:** MySQL 8.0 · **Herramienta:** MySQL Workbench · **Base:** `Floreria_Checador`

## 1. Cómo reconstruir la base

Ejecutar en MySQL Workbench, **en este orden** (File → Open SQL Script → rayo):

| # | Archivo | Qué hace |
| - | ------- | -------- |
| 1 | `01_esquema.sql` | Elimina y crea la base, tablas, restricciones y catálogos |
| 2 | `02_datos.sql` | Datos de prueba ficticios: 5 empleados, 2 semanas, 44 checadas, 4 ausencias |
| 3 | `03_rutinas.sql` | 3 funciones, 5 procedimientos y 8 triggers |
| 4 | `04_consultas_vistas.sql` | Genera los pagos de prueba, 2 vistas y 10 consultas documentadas |
| 5 | `05_pruebas.sql` | Casos de prueba válidos e inválidos, COMMIT/ROLLBACK, auditoría |

> **Cuidado:** `01_esquema.sql` empieza con `DROP DATABASE IF EXISTS Floreria_Checador;`.
> En una computadora con datos reales, los borra.

Los scripts 01–04 corren sin cambios manuales. En `05_pruebas.sql`, las pruebas marcadas
**POR SEPARADO** están comentadas porque provocan un error a propósito: se descomenta y
ejecuta una por una. Si se corrió `05` y se quiere repetir, reconstruir desde `01`.

**Requisito de permisos:** si MySQL tiene el binlog activo, crear funciones y triggers exige
`SUPER` o ejecutar antes `SET GLOBAL log_bin_trust_function_creators = 1;`.

**Estado de verificación:** los cinco scripts se ejecutaron en MariaDB 10.11 (compatible con
MySQL 8) y los resultados coinciden con los cálculos hechos a mano. **Aún no se han corrido en
MySQL 8 real:** hacerlo en Workbench antes de entregar y registrar lo observado.

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

Diagrama: `diagramas/diagrama_er.png` (fuente: `diagrama_er.mmd`).

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
| Tiempo extra | Primeros 540 min de la semana × 2; lo que pase × 3 (valores en `configuracion`) |

El descuento y el extra del ticket son la **suma de renglones**, cada uno redondeado a centavos.

## 4. Uso desde la página web

```sql
CALL sp_registrar_checada(id_empleado, ahora, 'Clave');   -- siguiente checada del día
CALL sp_generar_pago(id_empleado, id_semana, id_usuario); -- calcula (transacción) y puede regenerarse
CALL sp_ticket(id_empleado, id_semana);                   -- muestra el ticket sin recalcular
CALL sp_agregar_observacion(id_pago, 'texto');            -- permitido aun con el pago cerrado
CALL sp_cerrar_pago(id_pago);                             -- congela pago, ticket y checadas de la semana
```

`sp_generar_pago` corre dentro de `START TRANSACTION … COMMIT` con `ROLLBACK` si algo falla.
`sp_ticket` devuelve dos resultados: resumen y detalle por día.

## 5. Triggers

| Trigger | Efecto |
| ------- | ------ |
| `trg_registro_bi/bu/bd_cerrado` | Impiden insertar, modificar o borrar checadas de una semana cerrada |
| `trg_registro_au_auditoria` | Guarda hora anterior y nueva, usuario de la app y usuario de BD al corregir una checada |
| `trg_detalle_bi/bu/bd_cerrado` | Congelan el ticket de un pago cerrado |
| `trg_pago_bu_cerrado` | En un pago cerrado solo permite cambiar `observaciones` |

## 6. Resultados esperados con los datos de prueba

| Empleado | Semana 1 | Semana 2 |
| -------- | -------- | -------- |
| María López | $2,911.45 (85 min de comida de más) | $3,000.00 |
| Juan Pérez | $2,807.28 (desc. 630.22, extra 137.50) | $5,156.25 (extra 1,856.25) |
| Ana Torres | $3,000.00 | $2,468.75 (falta 531.25) |
| Luis Ramírez | $3,150.00 | $3,937.50 (extra 787.50) |

Total a pagar: semana 1 = $11,868.73, semana 2 = $14,562.50. Carla Mendoza (ingresó el
2026-11-09) no tiene pagos: sirve para probar `LEFT JOIN` y `NOT EXISTS`.

## 7. Pendientes con el dueño

| Pendiente | Hoy en el sistema |
| --------- | ----------------- |
| ¿Cuántos minutos acumulados de retardo descuentan 1 h y 2 h? | No descuentan. Basta llenar `retardo_min_para_1h` y `retardo_min_para_2h` en `configuracion` |
| ¿La falta justificada se paga completa? | Se **supone que sí** |
| Multiplicadores de tiempo extra | 2× y 3× (estilo Ley Federal del Trabajo); **confirmar con el contador**. No aplica topes legales diarios |
| ¿Quién corrige una checada olvidada? | Se supone dueño o encargado desde la página; queda auditado |
| Bonificaciones y vacaciones por antigüedad | Etapa 2 (fuera de alcance) |
| Checado por huella | Pospuesto; la BD ya tiene `id_huella` y método `Huella` |

## 8. Limitaciones y observaciones

- **48 h base vs 49 h de presencia.** El dueño confirmó que el sueldo se calcula sobre 48 h; por eso
  el pago es *sueldo base − tiempo perdido (+ extra)* y quien cumple todo cobra exactamente su base.
  Con seis faltas completas el descuento rebasa el sueldo y el salario se limita a $0.
- **`ausencia` no se bloquea** al cerrar una semana: se puede editar aunque el pago ya esté congelado.
- **`pago_semanal` guarda totales derivados** a propósito, como foto histórica del pago (justificar
  en la sección de normalización).
- **Días sin checada y sin ausencia** no se descuentan solos; la consulta Q5 los detecta.
- **Contraseñas y claves:** guardar solo hashes. Los de prueba son `HASH_DE_PRUEBA_NO_USAR`.

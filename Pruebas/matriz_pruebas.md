# Matriz de pruebas de la base de datos

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

## 1. Pruebas ejecutadas en el SGBD

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

## 2. Otras pruebas ejecutadas

| ID | Requisito | Entrada / pasos | Esperado | Observado | Estado |
| -- | --------- | --------------- | -------- | --------- | ------ |
| R01 | Regresión tras cada cambio | Reconstruir 01–04 y correr `verificacion_regresion.sql` | 22 comprobaciones en OK (objetos, 8 pagos, totales) | 22 OK, 0 FALLA, 0 advertencias (última corrida después de INC-05) | Pasa |
| S01 | Seguridad: usuario de mínimo privilegio | `05_usuario_app.sql` con un usuario temporal; 9 operaciones permitidas y 8 prohibidas | Permitidas funcionan; prohibidas dan 1142/1044 | 17/17 como se esperaba (UPDATE/INSERT/DELETE a pagos y auditoría, DROP, ALTER, CREATE, `mysql.user` y la base `veterinaria`, rechazados) | Pasa |
| S02 | Auditoría registra la cuenta real (INC-05) | Corrección de checada conectado como el usuario de la app | `usuario_bd` = cuenta de la app | `app_floreria_prueba@localhost` | Pasa |
| C01 | Contrato para la app (ADO.NET) | 9 llamadas de `CONTRATO_BD.md` con MySql.Data 26.10.0 en .NET 10 | Resultados y errores documentados | 9/9 OK (checada, error 1644 de domingo, pago, ticket con 2 resultados, vista, función, `bool`, 1644 al regenerar pago cerrado, 1142) | Pasa |
| B01 | Respaldo y restauración (base de prueba, antes de PEN-01) | `respaldo_restauracion.ps1` → base nueva → `CHECKSUM TABLE` | Mismas 11 tablas, 3 vistas, 8 rutinas, 12 triggers (los que había ese momento) y datos idénticos | 11/3/8/12; 11 de 11 tablas con checksum IGUAL; reintento sin `-Forzar` rechazado | Pasa |
| T01 | Transacción del llamador con `sp_generar_pago` | `START TRANSACTION; UPDATE; CALL sp_generar_pago; ROLLBACK` | (Comportamiento por documentar) | El `UPDATE` quedó guardado: COMMIT implícito (INC-04). Se documentó como regla para la app | Pasa (documentado) |

## 3. Reservadas para el equipo de la aplicación

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

## 4. Capturas que debe tomar el DBA (MySQL Workbench)

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

# Guion del video — parte de base de datos (5 a 8 minutos)

Todo se graba en **MySQL Workbench**, conectado a *Local instance MySQL80*. Antes de grabar,
reconstruir la base con `Sql/01` a `04` para que los datos sean los de la regresión.
Las partes de la aplicación web son **[A cargo de frontend/backend]**.

| Tiempo | Qué se ve en pantalla | Qué se dice |
| ------ | --------------------- | ----------- |
| 0:00–0:40 | Panel SCHEMAS con `belissimo` desplegado | "Esta es la base del reloj checador: 11 tablas, 3 vistas, 5 procedimientos, 3 funciones y 12 triggers." |
| 0:40–1:30 | `Diagramas/crows_foot.png` | Explicar empleado → checadas, ausencias y pagos; el pago como entidad asociativa; el ticket en `detalle_pago`. |
| 1:30–2:30 | **Operación válida:** P07, las 4 `CALL sp_registrar_checada(5, …)` | "El procedimiento decide solo qué checada sigue." Mostrar ENTRADA … SALIDA y la fila de Carla. |
| 2:30–3:15 | **Operación inválida:** P08, quinta checada, y P04, sueldo −100 | Mostrar `ERROR 1644 … ya tiene las cuatro checadas` y `ERROR 3819 … ck_empleado_sueldo`. "La base protege la regla aunque la página se equivoque." |
| 3:15–4:15 | **Procedimiento con transacción:** P10, `sp_generar_pago(2,1,2)` y `sp_ticket(2,1)` | Leer el ticket de Juan: falta −584.38, salida −34.38, comida −11.46, extra +137.50 → $2,807.28. Luego P20: error 1452 y el pago 8 queda igual (ROLLBACK). |
| 4:15–5:15 | **Trigger:** P13, corregir la entrada de María, y `SELECT` a `auditoria_registro` | "Nadie escribió en la auditoría: lo hizo el trigger, con la hora anterior, la nueva y quién la cambió." |
| 5:15–6:00 | **Pago cerrado:** P14 y P14e | Cerrar el pago 1 y mostrar `ERROR 1644 … solo se pueden agregar observaciones`. |
| 6:00–7:00 | **Persistencia:** cerrar la pestaña de conexión, abrir una nueva y ejecutar P16 | "Los datos siguen ahí: el pago 1 Cerrado, la corrección auditada y las checadas de Carla están guardados en MySQL, no en la página." |
| 7:00–7:40 | `Pruebas/matriz_pruebas.md` y `Pruebas/incidencias.md` | "32 pruebas pasan; encontramos y corregimos 5 incidencias reales al pasar a MySQL 8." |

**Consejo:** ejecutar cada sentencia con el cursor sobre ella y `Ctrl + Enter`, para que el
panel *Output* muestre el mensaje de cada una (los errores en rojo son parte de la demostración).

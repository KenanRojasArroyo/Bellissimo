# Validaciones que la interfaz también debe hacer

Las reglas críticas **ya las impone la base de datos**: si la app se equivoca, la base rechaza
la operación con un error (ver `CONTRATO_BD.md` §5). Aun así, la interfaz debe validarlas
**antes** de enviar los datos, para dar un mensaje claro y no depender de un error de MySQL.

| # | Pantalla | Validación en la interfaz | Respaldo en la BD |
| - | -------- | ------------------------- | ----------------- |
| V01 | Alta/edición de empleado | Sueldo base **semanal** obligatorio y **mayor a 0** (no por hora). | `ck_empleado_sueldo` (3819) |
| V02 | Alta/edición de empleado | Nombre y apellido paterno obligatorios, máximo 60 caracteres; teléfono máximo 20. | `NOT NULL`, `VARCHAR(60/20)` |
| V03 | Alta/edición de empleado | El puesto se elige de la tabla `puesto` (lista desplegable), no de un `enum` fijo. | `fk_empleado_puesto` (1452) |
| V04 | Baja de empleado | Ofrecer "Desactivar" (`activo = 0`) en lugar de borrar si ya checó o cobró. | `ON DELETE RESTRICT` (1451) |
| V05 | Checador | Validar la clave personal contra el **hash** antes de llamar `sp_registrar_checada`; no permitir domingo; usar la hora del servidor, no la que escriba el usuario. | `sp_registrar_checada` (1644) |
| V06 | Checador | Deshabilitar el botón cuando ya hay 4 checadas del día. | `sp_registrar_checada` (1644) |
| V07 | Corrección de checada | Solo dueño o encargado; horas en orden (entrada ≤ salida a comer ≤ regreso ≤ salida) y del mismo día; enviar `metodo = 'Manual'` y `modificado_por`. | `ck_registro_orden`, `ck_registro_fecha` (3819); auditoría por trigger |
| V08 | Ausencias | Un tipo por día (Justificada solo por salud, Injustificada, Vacaciones); no repetir el mismo día. | `ENUM`, `uq_ausencia_dia` (1062) |
| V09 | Ausencias / checadas | Si `fn_semana_cerrada(id_empleado, fecha) = 1`, mostrar la semana **en solo lectura**. | Triggers de semana cerrada (1644) |
| V10 | Pagos | Antes de generar o cerrar, mostrar `v_dias_sin_checada` y pedir que se capture la ausencia que falte. | — (la BD solo avisa, no descuenta sola) |
| V11 | Pagos | Pedir **confirmación** antes de `sp_cerrar_pago` ("ya no se podrá modificar"). | Triggers de pago cerrado (1644) |
| V12 | Pagos | No envolver `sp_generar_pago` en una transacción de la app. | INC-04 (COMMIT implícito) |
| V13 | Configuración | Valores numéricos enteros y positivos (horas base, tolerancia, comida, umbral); multiplicadores ≥ 1; las reglas de retardo pueden quedar vacías (pendiente). | `sp_generar_pago` valida que existan (1644) |
| V14 | Usuarios / login | Contraseña guardada con hash (por ejemplo `PasswordHasher` de ASP.NET Core); `nombre_usuario` único; solo usuarios `activo = 1` entran. | `UNIQUE` (1062); la BD no puede verificar que sea hash |
| V15 | Todas | Usar **siempre** parámetros (`@param`), nunca concatenar texto en el SQL. | Previene inyección SQL |
| V16 | Ausencias / checador | No ofrecer registrar ausencia en un día en que el empleado ya checó, ni checar en un día con ausencia (mostrar la ausencia y la opción de borrarla si la semana está abierta). | `trg_registro_bi/bu_ausencia`, `trg_ausencia_bi/bu_checada` (1644) |

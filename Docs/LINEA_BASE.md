# Línea base — Backend

## Equipo
- Frontend: Kenan Rojas Arroyo | KenanRojasArroyo
- Backend / Líder: Troy Leonardo Moreno Calderón | Troy2404
- DBA / Pruebas: Jose Antonio Montoya Garcia | josemontoya83-del

## Estado inicial
- Fecha: 2026-11-22
- Commit de main: `2171fd2` (Merge pull request #7 from KenanRojasArroyo/frontend)
- Compila: No (5 errores `CS0234`: no existe el espacio de nombres `Bellissimo.Negocios`)
- Ejecuta: No (no compila)
- MySQL: Sí (base `belissimo` con scripts `Sql/01`–`05` del DBA), pero la aplicación todavía no se conecta
- Personal: No (las vistas existen, pero no hay capa de Negocios ni de Datos)

## Problemas conocidos antes de corregir

- `Bellissimo.Datos` y `Bellissimo.Negocios` están vacíos: no existen `PersonalNegocio`,
  `ReglaNegocioException`, ni las clases de acceso a datos. Las páginas de `Personal`
  ya los usan, por eso la solución no compila.
- `Bellissimo.Datos` no referencia el paquete `MySql.Data`.
- `Program.cs` no lee `ConnectionStrings:Bellissimo` ni inicializa Negocios.
- Los puestos de `Personal/Nuevo` y `Personal/Editar` están escritos a mano
  (`Recepcionista`, `Repartidor`, `Encargado de Florista`); en la tabla `puesto` el
  `id_puesto = 3` se llama `Florista`.
- En `Personal/Eliminar.cshtml.cs`, el `OnPost` carga el empleado **antes** del `try`.
  Si MySQL falla en ese momento, la aplicación termina en una página 500 en lugar de
  mostrar un mensaje amigable.
- La entidad `Personal` usa nombres distintos a la tabla `empleado`
  (`IdPersonal` ↔ `id_empleado`, `SueldoBase` ↔ `sueldo_base_semanal`,
  `FechaRegistro` ↔ `fecha_ingreso`); la traducción debe hacerse en Datos.

## Estado final

> Completar después de integrar `backend` en `main` y repetir la aceptación.

- Fecha:
- Commit de main aceptado:
- Compila: Sí (0 errores, 0 advertencias en la rama `backend`)
- Ejecuta: Sí (con y sin base; sin base muestra mensajes amigables)
- MySQL: Sí (User Secrets `ConnectionStrings:Bellissimo`, usuario `app_floreria`)
- Personal: APROBADO (integración 26/26 OK en `PRUEBAS_BACKEND.md`)

## Problemas resueltos

| Problema | Rol | Corrección |
| --- | --- | --- |
| La solución no compilaba: faltaban `PersonalNegocio` y `ReglaNegocioException` | Backend | Capas Datos y Negocios de Personal creadas |
| `Bellissimo.Datos` no referenciaba `MySql.Data` | Backend | Paquete `MySql.Data 26.10.0` agregado |
| `Program.cs` no leía la cadena de conexión | Backend | Lee `ConnectionStrings:Bellissimo` de User Secrets, arranque no bloqueante |
| Puestos escritos a mano y con nombre distinto al de la tabla | Backend | Nuevo / Editar usan `PuestoNegocio.Listar()` |
| El `OnPost` de `Personal/Eliminar` cargaba datos fuera del `try` (página 500 si fallaba MySQL) | Backend | Carga dentro del `try` con mensaje amigable |

## Problemas pendientes

- Ninguno dentro del alcance obligatorio (módulo Personal).

# Bellissimo — Sistema de gestión para florería

Proyecto académico de
Ambientes de Programación Visual y Base de Datos
(UABC — FIAD, ISyTE).

## Tecnologías

- ASP.NET Core Razor Pages (.NET 10)
- C#
- Bootstrap
- MySQL 8.0
- MySql.Data (ADO.NET)
- GitHub Desktop

## Arquitectura

Web → Negocios → Datos → MySQL

| Proyecto | Responsabilidad |
| --- | --- |
| `Bellissimo.Web` | Interfaz con Razor Pages y Bootstrap |
| `Bellissimo.Negocios` | Reglas y validaciones |
| `Bellissimo.Datos` | Acceso a MySQL con consultas parametrizadas |
| `Bellissimo.Entidades` | Modelos compartidos (`Personal`, `Puesto`, `Ausencia`, `RegistroAsistencia`, `Semana`, `Ticket`, …) |

Más detalle en [`Docs/ARQUITECTURA.md`](Docs/ARQUITECTURA.md).

## Módulos

| Módulo | Backend (Negocios + Datos) | Páginas |
| --- | :---: | :---: |
| Personal: listar, buscar, registrar, editar (baja lógica), detalle y eliminar | Listo | Listas |
| Checador con clave personal | Listo | Listas |
| Ausencias (CRUD) | Listo | Listas |
| Correcciones de checadas (CRUD) y reporte de asistencia | Listo | Listas |
| Puestos (alta y edición) | Listo | Listas |
| Semanas y pagos: generar, ticket, cerrar, observaciones, días sin checada | Listo | Listas |

## Requisitos

- Visual Studio 2026 (o el SDK de .NET 10)
- .NET 10
- MySQL Server 8.0
- MySQL Workbench
- Git o GitHub Desktop

```
bash
git clone <URL_DEL_REPOSITORIO>
cd bellissimo_floreria
```

Para cambiar a la rama de desarrollo de interfaz antes de su integración a main:

```
git checkout frontend
```

## Base de datos

Ejecutar en orden `Sql/01_esquema.sql` → `Sql/05_usuario_app.sql`
(el `01` elimina y vuelve a crear la base `belissimo`).
En `05_usuario_app.sql` cambiar la contraseña **sólo en la copia local**.
Detalles en [`Sql/README.md`](Sql/README.md) y [`Sql/CONTRATO_BD.md`](Sql/CONTRATO_BD.md).

## Configuración local

La conexión utiliza User Secrets con el usuario `app_floreria`.
No se publican contraseñas.

En Visual Studio: clic derecho sobre `Bellissimo.Web` →
**Manage User Secrets**, y agregar:

```json
{
  "ConnectionStrings": {
    "Bellissimo": "Server=localhost;Port=3306;Database=belissimo;User ID=app_floreria;Password=<contraseña>;CharSet=utf8mb4;SslMode=Preferred"
  }
}
```

O desde la terminal:

```powershell
cd Bellissimo.Web
dotnet user-secrets set "ConnectionStrings:Bellissimo" "Server=localhost;Port=3306;Database=belissimo;User ID=app_floreria;Password=<contraseña>;CharSet=utf8mb4;SslMode=Preferred"
cd ..
```

## Ejecución
Desde Visual Studio:

1. Establecer Bellissimo.Web como proyecto de inicio (Set as Startup Project).
2. Presionar F5 o Ctrl + F5 (Visual Studio restaurará automáticamente los paquetes NuGet como MySql.Data y abrirá el navegador).

```
dotnet restore
dotnet run --project Bellissimo.Web
```

Abrir en el navegador la URL local mostrada en consola (https://localhost:7xxx o http://localhost:5xxx).

## Documentación

| Documento | Contenido |
| --- | --- |
| [`Docs/ARQUITECTURA.md`](Docs/ARQUITECTURA.md) | Capas, flujos y manejo de errores |
| [`Docs/PRUEBAS_BACKEND.md`](Docs/PRUEBAS_BACKEND.md) | Revisión de Backend |
| [`Docs/PRUEBAS_FRONTEND.md`](Docs/PRUEBAS_FRONTEND.md) | Revisión de Frontend |
| [`Docs/PENDIENTES.md`](Docs/PENDIENTES.md) | Evolución futura |
| [`Pruebas/matriz_pruebas.md`](Pruebas/matriz_pruebas.md) | Pruebas de base de datos |

## Equipo

- Frontend: Kenan Rojas Arroyo | KenanRojasArroyo
- Backend / Líder: Troy Leonardo Moreno Calderón | Troy2404
- DBA / Pruebas: Jose Antonio Montoya Garcia | josemontoya83-del

## Ramas de trabajo

- `main`: versión estable e integrada
- `backend`
- `frontend`
- `dba`

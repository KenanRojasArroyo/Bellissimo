# Plantilla de cadena de conexión

La aplicación se conecta con el usuario de mínimo privilegio **`app_floreria`**
(`Sql/05_usuario_app.sql`), nunca con `root`.

```text
Server=localhost;Port=3306;Database=belissimo;User ID=app_floreria;Password=<CONTRASEÑA>;CharSet=utf8mb4;SslMode=Preferred
```

Esta cadena (con un usuario de los mismos permisos) se probó con MySql.Data 26.10.0 en .NET 10.

## Dónde guardarla: NUNCA en `appsettings.json`

`appsettings.json` se sube a GitHub. La contraseña va en **user-secrets** (desarrollo) o en una
**variable de entorno** (servidor). El proyecto `Bellissimo.Web` ya tiene `UserSecretsId`.

### Desarrollo (cada integrante en su computadora)

```powershell
cd Bellissimo.Web
dotnet user-secrets set "ConnectionStrings:Bellissimo" "Server=localhost;Port=3306;Database=belissimo;User ID=app_floreria;Password=<CONTRASEÑA>;CharSet=utf8mb4;SslMode=Preferred"
dotnet user-secrets list      # comprobar (muestra el valor solo en tu máquina)
```

Los secretos quedan en `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`, fuera del
repositorio.

### Servidor / computadora de la florería

```powershell
# variable de entorno del sistema (doble guion bajo = ':' de la configuración)
setx ConnectionStrings__Bellissimo "Server=localhost;Port=3306;Database=belissimo;User ID=app_floreria;Password=<CONTRASEÑA>;CharSet=utf8mb4;SslMode=Preferred"
```

### Leerla en el código

```csharp
// Program.cs / Bellissimo.Datos
var cs = builder.Configuration.GetConnectionString("Bellissimo")
         ?? throw new InvalidOperationException("Falta la cadena de conexión 'Bellissimo'.");
```

En `appsettings.json` solo puede quedar la **clave vacía** como recordatorio:

```json
{ "ConnectionStrings": { "Bellissimo": "" } }
```

## Paquete NuGet

```powershell
cd Bellissimo.Datos
dotnet add package MySql.Data
```

<#
=====================================================================
 FLORERÍA - RELOJ CHECADOR Y PAGO SEMANAL
 Archivo : respaldo_restauracion.ps1   (PowerShell, Windows)

 Respalda y restaura la base con mysqldump / mysql, INCLUYENDO
 procedimientos, funciones, triggers y vistas.

 USO
   # Respaldo (crea respaldos\belissimo_respaldo_AAAAMMDD_HHMM.sql)
   .\Sql\respaldo_restauracion.ps1 -Accion Respaldar

   # Restaurar en una base NUEVA para revisar el respaldo sin tocar la buena
   .\Sql\respaldo_restauracion.ps1 -Accion Restaurar -Archivo .\respaldos\X.sql -Base belissimo_restaurada

   # Restaurar ENCIMA de la base real (pide -Forzar porque reemplaza tablas)
   .\Sql\respaldo_restauracion.ps1 -Accion Restaurar -Archivo .\respaldos\X.sql -Base belissimo -Forzar

 CONTRASEÑA: el script la pide en pantalla (no se muestra) o usa la variable
 de entorno MYSQL_PWD si ya existe en la sesión. NUNCA se guarda en un archivo.

 SEGURIDAD
 - Los respaldos contienen datos de empleados y hashes: la carpeta respaldos\
   está en Sql/.gitignore y NO se sube a GitHub.
 - Guardar una copia fuera de la computadora (USB o nube privada del dueño).
 - Probar la restauración en una base aparte de vez en cuando (un respaldo que
   nunca se restauró no está comprobado).
=====================================================================
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet('Respaldar', 'Restaurar')][string]$Accion,
    [string]$Base = 'belissimo',
    [string]$Archivo,
    [string]$Usuario = 'root',
    [string]$Carpeta = (Join-Path $PSScriptRoot 'respaldos'),
    [switch]$Forzar
)

$ErrorActionPreference = 'Stop'
$bin = 'C:\Program Files\MySQL\MySQL Server 8.0\bin'
$mysql = Join-Path $bin 'mysql.exe'
$mysqldump = Join-Path $bin 'mysqldump.exe'

if ($Base -notmatch '^[A-Za-z0-9_]+$') { throw "Nombre de base no válido: $Base" }

$pusoClave = $false
if (-not $env:MYSQL_PWD) {
    $seguro = Read-Host "Contraseña de MySQL para '$Usuario'" -AsSecureString
    $env:MYSQL_PWD = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro))
    $pusoClave = $true
}

try {
    if ($Accion -eq 'Respaldar') {
        New-Item -ItemType Directory -Force $Carpeta | Out-Null
        if (-not $Archivo) {
            $Archivo = Join-Path $Carpeta ("{0}_respaldo_{1}.sql" -f $Base, (Get-Date -Format 'yyyyMMdd_HHmm'))
        }
        # Sin --databases: el respaldo no trae CREATE DATABASE/USE, así se puede
        # restaurar en cualquier base (por ejemplo una de prueba).
        & $mysqldump -u $Usuario --default-character-set=utf8mb4 `
            --single-transaction --routines --triggers --events `
            --set-gtid-purged=OFF --no-tablespaces `
            --result-file="$Archivo" $Base
        if ($LASTEXITCODE -ne 0) { throw "mysqldump terminó con código $LASTEXITCODE" }
        $kb = [math]::Round((Get-Item $Archivo).Length / 1KB, 1)
        Write-Host "Respaldo creado: $Archivo ($kb KB)"
    }
    else {
        if (-not $Archivo -or -not (Test-Path $Archivo)) { throw "Indica -Archivo con un respaldo existente." }
        $tablas = & $mysql -u $Usuario -N -B -e "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA='$Base' AND TABLE_TYPE='BASE TABLE'"
        if ([int]$tablas -gt 0 -and -not $Forzar) {
            throw "La base '$Base' ya tiene $tablas tablas. Usa -Forzar para reemplazarlas o restaura en otra base."
        }
        & $mysql -u $Usuario -e "CREATE DATABASE IF NOT EXISTS $Base CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci"
        $ruta = (Resolve-Path $Archivo).Path -replace '\\', '/'
        & $mysql -u $Usuario --default-character-set=utf8mb4 $Base -e "source $ruta"
        if ($LASTEXITCODE -ne 0) { throw "mysql terminó con código $LASTEXITCODE" }
        & $mysql -u $Usuario -t -e @"
SELECT
 (SELECT COUNT(*) FROM information_schema.TABLES   WHERE TABLE_SCHEMA='$Base' AND TABLE_TYPE='BASE TABLE') AS tablas,
 (SELECT COUNT(*) FROM information_schema.VIEWS    WHERE TABLE_SCHEMA='$Base') AS vistas,
 (SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA='$Base') AS rutinas,
 (SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA='$Base') AS triggers_
"@
        Write-Host "Restauración terminada en la base '$Base'."
    }
}
finally {
    if ($pusoClave) { Remove-Item Env:MYSQL_PWD -ErrorAction SilentlyContinue }
}

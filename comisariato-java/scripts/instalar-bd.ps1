param(
    [string]$Servidor = 'localhost',
    [string]$Base = 'Comisariato',
    [switch]$AutenticacionSql
)
$ErrorActionPreference = 'Stop'
if ($Base -notmatch '^Comisariato[A-Za-z0-9_]*$') {
    throw 'El nombre de base debe comenzar con Comisariato y contener solo letras, numeros o guion bajo.'
}
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$builder.DataSource = $Servidor
$builder.InitialCatalog = 'master'
$builder.ConnectTimeout = 10
$builder.Encrypt = $true
# Solo para la instalacion academica local con certificado autofirmado.
$builder.TrustServerCertificate = $true
if ($AutenticacionSql) {
    $credential = Get-Credential -Message 'Credenciales SQL Server con permiso de instalacion'
    $builder.UserID = $credential.UserName
    $builder.Password = $credential.GetNetworkCredential().Password
} else { $builder.IntegratedSecurity = $true }

function Invoke-CommandSql([System.Data.SqlClient.SqlConnection]$Connection, [string]$Sql) {
    $command = $Connection.CreateCommand()
    try { $command.CommandText=$Sql; $command.CommandTimeout=60; [void]$command.ExecuteNonQuery() }
    finally { $command.Dispose() }
}
function Invoke-Batches([System.Data.SqlClient.SqlConnection]$Connection, [string]$Text) {
    foreach ($batch in [regex]::Split($Text, '(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')) {
        if (-not [string]::IsNullOrWhiteSpace($batch)) { Invoke-CommandSql $Connection $batch }
    }
}
$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    Invoke-CommandSql $connection "IF DB_ID(N'$Base') IS NULL EXEC(N'CREATE DATABASE [$Base]');"
    $connection.ChangeDatabase($Base)
    $command=$connection.CreateCommand()
    $command.CommandText="SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0"
    $tables=[int]$command.ExecuteScalar()
    $command.Dispose()
    if ($tables -eq 0) {
        $original=Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot '../database/01_base_original.sql')
        $start=$original.IndexOf('CREATE TABLE dbo.Categorias')
        if ($start -lt 0) { throw 'No se reconoce el script original.' }
        Invoke-Batches $connection $original.Substring($start)
        Write-Host 'Tablas y datos iniciales instalados.'
    } else {
        Write-Host 'La base ya contiene tablas; se conserva el esquema y los datos existentes.'
    }
    Invoke-Batches $connection (Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot '../database/02_objetos_api.sql'))
    Invoke-Batches $connection (Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot '../database/03_rol_aplicacion.sql'))
    Write-Host "Objetos del taller instalados en $Servidor / $Base."
} finally { $connection.Dispose(); $builder.Clear() }

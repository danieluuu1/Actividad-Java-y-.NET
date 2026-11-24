param([string]$Servidor = 'localhost', [int]$Puerto=1433, [string]$Base='Comisariato')
$ErrorActionPreference='Stop'
if (-not $env:DB_URL) {
    $env:DB_URL="jdbc:sqlserver://${Servidor}:${Puerto};databaseName=$Base;encrypt=true;trustServerCertificate=true;loginTimeout=10"
}
if (-not $env:DB_USER) { $env:DB_USER=Read-Host 'Usuario de SQL Server' }
if (-not $env:DB_PASSWORD) {
    $secure=Read-Host 'Contrasena de SQL Server' -AsSecureString
    $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $env:DB_PASSWORD=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
& (Join-Path $PSScriptRoot 'ejecutar.ps1')
exit $LASTEXITCODE

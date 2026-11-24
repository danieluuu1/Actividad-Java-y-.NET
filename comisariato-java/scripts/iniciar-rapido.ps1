param(
    [string]$Servidor = 'localhost',
    [int]$Puerto = 1434,
    [string]$Base = 'Comisariato'
)

$jar = Join-Path $PSScriptRoot '../dist/java-api-1.0.0.jar'
if (!(Test-Path -LiteralPath $jar)) { $jar = Join-Path $PSScriptRoot '../java-api/target/java-api-1.0.0.jar' }

$nativePath = Join-Path $PSScriptRoot '../native'

$javaCmd = 'java'
$candidates = @(
    "$env:JAVA_HOME\bin\java.exe",
    'C:\Users\DANIEL\Documents\Codex\2026-11-24\qu\work\tools\jdk-21.0.13.1+2\bin\java.exe',
    'C:\Users\DANIEL\.jdks\openjdk-26.0.2\bin\java.exe',
    'C:\Users\DANIEL\.jdks\openjdk-26.0.1\bin\java.exe'
)
foreach ($j in $candidates) {
    if ($j -and (Test-Path $j)) { $javaCmd = $j; break }
}

$env:DB_URL = "jdbc:sqlserver://${Servidor}:${Puerto};databaseName=$Base;encrypt=true;trustServerCertificate=true;integratedSecurity=true;loginTimeout=10"
Write-Host "Iniciando Comisariato API..." -ForegroundColor Cyan
Write-Host "Java: $javaCmd" -ForegroundColor Gray
Write-Host "Base: $Base en ${Servidor}:${Puerto}" -ForegroundColor Gray
Write-Host "Swagger UI: http://localhost:8080/swagger-ui.html" -ForegroundColor Green
Write-Host "Presione Ctrl+C para detener.`n" -ForegroundColor Yellow

& $javaCmd "-Djava.library.path=$nativePath" -jar $jar
exit $LASTEXITCODE

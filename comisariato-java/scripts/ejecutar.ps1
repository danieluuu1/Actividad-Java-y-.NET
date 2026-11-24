$ErrorActionPreference = 'Stop'
$jar = Join-Path $PSScriptRoot '../java-api/target/java-api-1.0.0.jar'
if (!(Test-Path -LiteralPath $jar)) { $jar = Join-Path $PSScriptRoot '../dist/java-api-1.0.0.jar' }
if (!(Test-Path -LiteralPath $jar)) { throw 'Primero compile dentro de java-api: .\mvnw.cmd clean verify' }
& java -jar $jar
exit $LASTEXITCODE

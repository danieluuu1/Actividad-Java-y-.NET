param([string]$ApiUrl = 'http://localhost:8080')

$source = Join-Path $PSScriptRoot '../java-api/src/main/java/edu/comisariato/api/menu/MenuConsola.java'

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

& $javaCmd --source 17 $source $ApiUrl
exit $LASTEXITCODE

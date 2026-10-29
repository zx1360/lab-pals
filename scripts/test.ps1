param(
    [string]$BuildDirectory = '.build/test',
    [string]$ReportDirectory = 'artifacts/verification/current',
    [switch]$Performance
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$buildPath = Get-ProjectPath $BuildDirectory
$reportPath = Get-ProjectPath $ReportDirectory
& (Get-ProjectPath 'build.ps1') -OutputDirectory $buildPath -IncludeDiagnostics | Out-Host
New-Item -ItemType Directory -Force -Path $reportPath | Out-Null
$diagnostic = Join-Path $buildPath 'LabPals.Diagnostics.exe'
Invoke-Diagnostic $diagnostic '--self-test' (Join-Path $reportPath 'self-test.txt')
if ($Performance) {
    Invoke-Diagnostic $diagnostic '--motion-test' (Join-Path $reportPath 'motion.txt')
    Invoke-Diagnostic $diagnostic '--burst-test' (Join-Path $reportPath 'burst.txt')
}
$checks = @(Select-String -LiteralPath (Join-Path $reportPath 'self-test.txt') -Pattern '^PASS ').Count
$result = [ordered]@{
    version = Get-AppVersion
    checkedAt = [DateTimeOffset]::Now.ToString('o')
    applicationSha256 = (Get-FileHash -LiteralPath (Join-Path $buildPath 'LabPals.exe') -Algorithm SHA256).Hash
    diagnosticsSha256 = (Get-FileHash -LiteralPath $diagnostic -Algorithm SHA256).Hash
    catalogSha256 = (Get-FileHash -LiteralPath (Get-ProjectPath 'assets/characters.json') -Algorithm SHA256).Hash
    passedChecks = $checks
    performanceIncluded = [bool]$Performance
}
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $reportPath 'validation.json') -Encoding UTF8
Write-Output ("PASS: {0} checks. Reports: {1}" -f $checks, $reportPath)

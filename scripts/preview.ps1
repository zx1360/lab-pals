param(
    [string]$OutputDirectory = 'artifacts/previews/current',
    [string]$BuildDirectory = '.build/preview',
    [switch]$UseExistingBuild,
    [switch]$Frames,
    [switch]$EncodeAnimations,
    [string]$SharpModule
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$buildPath = Get-ProjectPath $BuildDirectory
$outputPath = Get-ProjectPath $OutputDirectory
if (-not $UseExistingBuild) { & (Get-ProjectPath 'build.ps1') -OutputDirectory $buildPath -IncludeDiagnostics | Out-Host }
$diagnostic = Join-Path $buildPath 'LabPals.Diagnostics.exe'
if (-not (Test-Path -LiteralPath $diagnostic)) { throw 'Build diagnostics first or omit -UseExistingBuild.' }
$mode = '--preview'
if ($Frames -or $EncodeAnimations) { $mode = '--preview-frames' }
Invoke-Diagnostic $diagnostic $mode $outputPath
if ($EncodeAnimations) {
    $node = Get-Command node -ErrorAction Stop
    foreach ($animation in @(@('typing-frames','键盘反馈.gif'),@('interaction-frames','交互与入睡.gif'))) {
        $arguments = @((Get-ProjectPath 'tools/preview/encode-gif.cjs'), (Join-Path $outputPath $animation[0]), (Join-Path $outputPath $animation[1]))
        if ($SharpModule) { $arguments += $SharpModule }
        & $node.Source @arguments
        if ($LASTEXITCODE -ne 0) { throw "Animation encoding failed: $($animation[1])" }
    }
}
Write-Output $outputPath

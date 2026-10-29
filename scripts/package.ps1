param([switch]$Performance)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$version = Get-AppVersion
$buildPath = Get-ProjectPath ".build/package/$version"
$reportPath = Get-ProjectPath "artifacts/verification/$version"
$previewPath = Get-ProjectPath "artifacts/previews/$version"
& (Get-ProjectPath 'scripts/test.ps1') -BuildDirectory $buildPath -ReportDirectory $reportPath -Performance:$Performance | Out-Host
& (Get-ProjectPath 'scripts/preview.ps1') -OutputDirectory $previewPath -BuildDirectory $buildPath -UseExistingBuild | Out-Host
$validation = Get-Content -LiteralPath (Join-Path $reportPath 'validation.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$appFile = Join-Path $buildPath 'LabPals.exe'
if ((Get-FileHash -LiteralPath $appFile -Algorithm SHA256).Hash -ne $validation.applicationSha256) { throw 'The tested binary has changed.' }
$releasePath = Get-ProjectPath "releases/$version"
# Require a clean new staging folder; replacing a ZIP never silently retains obsolete staged files.
if (Test-Path -LiteralPath $releasePath) { $releasePath = Get-ProjectPath ("releases/{0}-{1}" -f $version, [DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff')) }
New-Item -ItemType Directory -Force -Path $releasePath,(Join-Path $releasePath 'previews'),(Join-Path $releasePath 'verification') | Out-Null
Copy-Item -LiteralPath $appFile -Destination (Join-Path $releasePath 'LabPals.exe')
Copy-Item -LiteralPath (Get-ProjectPath 'docs/user-guide.md') -Destination (Join-Path $releasePath '使用说明.md')
Copy-Item -LiteralPath (Get-ProjectPath 'LICENSE'),(Get-ProjectPath 'NOTICE') -Destination $releasePath
foreach ($name in @('角色一览.png','点击回应.png','入睡姿势.png','动作预览.png','设置面板.png')) {
    Copy-Item -LiteralPath (Join-Path $previewPath $name) -Destination (Join-Path $releasePath 'previews')
}
foreach ($name in @('self-test.txt','validation.json')) {
    Copy-Item -LiteralPath (Join-Path $reportPath $name) -Destination (Join-Path $releasePath 'verification')
}
if ($Performance) { Copy-Item -LiteralPath (Join-Path $reportPath 'motion.txt') -Destination (Join-Path $releasePath 'verification') }
$releaseFiles = @(Get-ChildItem -LiteralPath $releasePath -Recurse -File | Sort-Object FullName)
$hashLines = foreach ($file in $releaseFiles) {
    $relative = $file.FullName.Substring($releasePath.Length + 1).Replace('\','/')
    "{0}  {1}" -f (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
}
$hashLines | Set-Content -LiteralPath (Join-Path $releasePath 'SHA256SUMS.txt') -Encoding ASCII
$zipPath = Get-ProjectPath "releases/LabPals-v$version-Windows.zip"
Compress-Archive -Path (Join-Path $releasePath '*') -DestinationPath $zipPath -Force
# Read the final ZIP and verify that its program is exactly the binary that passed checks.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entry = $zip.GetEntry('LabPals.exe')
    if ($null -eq $entry) { throw 'ZIP is missing LabPals.exe.' }
    $stream = $entry.Open(); $hasher = [Security.Cryptography.SHA256]::Create()
    try { $zipHash = [BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-','') }
    finally { $stream.Dispose(); $hasher.Dispose() }
    if ($zipHash -ne $validation.applicationSha256) { throw 'ZIP application differs from the tested binary.' }
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName -match '(^|/)(archive|artwork|tools|node_modules|\.build)/|\.Diagnostics\.exe$') { throw "Unexpected release entry: $($entry.FullName)" }
    }
} finally { $zip.Dispose() }
Write-Output $zipPath

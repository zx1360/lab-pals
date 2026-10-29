param(
    [string]$OutputDirectory = 'dist',
    [switch]$IncludeDiagnostics
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'scripts/Common.ps1')
$catalog = Test-CharacterAssets
$version = Get-AppVersion
$outputPath = Get-ProjectPath $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Windows .NET Framework 4.x compiler is required.' }
$outputExe = Join-Path $outputPath 'LabPals.exe'
if (Get-Process LabPals -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $outputExe }) {
    throw 'This build destination is running. Exit the pet or choose another -OutputDirectory.'
}
$hashAlgorithm = [Security.Cryptography.SHA256]::Create()
try { $buildKey = [BitConverter]::ToString($hashAlgorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($outputPath))).Replace('-','').Substring(0,16) }
finally { $hashAlgorithm.Dispose() }
$generatedPath = Get-ProjectPath ".build/generated/$buildKey"
New-Item -ItemType Directory -Force -Path $generatedPath | Out-Null
$utf8 = New-Object Text.UTF8Encoding($true)
$enumItems = @(); $definitions = @()
foreach ($item in $catalog.characters) {
    $enumItems += ('        {0} = {1}' -f $item.id, $item.value)
    $texts = @('name','shortName','english','traits','reactionCaption') | ForEach-Object { ConvertTo-CSharpString ([string]$item.$_) }
    $numbers = @($item.leftWrist) + @($item.rightWrist) | ForEach-Object { ([double]$_).ToString('0.0######',[Globalization.CultureInfo]::InvariantCulture) + 'f' }
    $definitions += ('            new CharacterDefinition(Character.{0}, {1}, 0x{2}, {3})' -f $item.id, ($texts -join ', '), $item.accent.Substring(1), ($numbers -join ', '))
}
$catalogSource = "// Generated from assets/characters.json; edit the JSON, not this file.`r`nnamespace LabPals {`r`n    public enum Character {`r`n" + ($enumItems -join ",`r`n") + "`r`n    }`r`n    internal static class CharacterData { internal static CharacterDefinition[] Create() { return new[] {`r`n" + ($definitions -join ",`r`n") + "`r`n        }; } }`r`n}`r`n"
$catalogFile = Join-Path $generatedPath 'CharacterData.g.cs'
[IO.File]::WriteAllText($catalogFile, $catalogSource, $utf8)
$versionFile = Join-Path $generatedPath 'VersionInfo.g.cs'
[IO.File]::WriteAllText($versionFile, ('using System.Reflection; [assembly: AssemblyVersion("{0}.0")] [assembly: AssemblyFileVersion("{0}.0")]' -f $version), $utf8)
$manifestFile = Join-Path $generatedPath 'app.manifest'
$manifest = (Get-Content -LiteralPath (Get-ProjectPath 'assets/app/app.manifest') -Raw -Encoding UTF8).Replace('__VERSION__', "$version.0")
[IO.File]::WriteAllText($manifestFile, $manifest, $utf8)
$sources = @(Get-ChildItem -LiteralPath (Get-ProjectPath 'src') -Recurse -File -Filter '*.cs' | Sort-Object FullName | ForEach-Object { $_.FullName }) + @($catalogFile,$versionFile)
$arguments = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/utf8output','/warn:4',"/win32manifest:$manifestFile",("/win32icon:" + (Get-ProjectPath 'assets/app/lab-pals.ico')),
    '/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Xml.dll')
$arguments += ('/resource:{0},LabPals.App.Icon' -f (Get-ProjectPath 'assets/app/lab-pals.ico'))
foreach ($item in $catalog.characters) {
    foreach ($suffix in $catalog.layers) {
        $name = "$($item.id)$suffix.png"
        $arguments += ('/resource:{0},LabPals.Art.{1}' -f (Get-ProjectPath "assets/characters/$name"), $name)
    }
}
& $compiler @arguments "/out:$outputExe" '/main:LabPals.Program' @sources
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
if ($IncludeDiagnostics) {
    $diagnosticSources = @(Get-ChildItem -LiteralPath (Get-ProjectPath 'diagnostics') -Recurse -File -Filter '*.cs' | Sort-Object FullName | ForEach-Object { $_.FullName })
    $diagnosticExe = Join-Path $outputPath 'LabPals.Diagnostics.exe'
    & $compiler @arguments "/out:$diagnosticExe" '/main:LabPals.DiagnosticsProgram' @sources @diagnosticSources
    if ($LASTEXITCODE -ne 0) { throw 'Diagnostics compilation failed.' }
}
Write-Output $outputExe

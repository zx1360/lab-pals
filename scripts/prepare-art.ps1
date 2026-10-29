param([string]$Character, [string]$SharpModule)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$node = Get-Command node -ErrorAction Stop
$module = 'sharp'
if ($SharpModule) { $module = $SharpModule }
$arguments = @((Get-ProjectPath 'tools/art/prepare-sprites.cjs'),$module)
if ($Character) { $arguments += $Character }
& $node.Source @arguments
if ($LASTEXITCODE -ne 0) { throw 'Offline art preparation failed.' }
& (Get-ProjectPath 'scripts/validate-assets.ps1')

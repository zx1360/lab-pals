$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$catalog = Test-CharacterAssets
Write-Output ("Validated {0} characters and {1} runtime images." -f @($catalog.characters).Count, (@($catalog.characters).Count * 6))

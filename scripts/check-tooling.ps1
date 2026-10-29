$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')
$scratch = Get-ProjectPath '.build/catalog-checks'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
$catalogText = Get-Content -LiteralPath (Get-ProjectPath 'assets/characters.json') -Raw -Encoding UTF8
$mutations = [ordered]@{
    'duplicate-id' = { param($c) $c.characters[1].id = $c.characters[0].id }
    'duplicate-value' = { param($c) $c.characters[1].value = $c.characters[0].value }
    'missing-layer' = { param($c) $c.layers = @('','-rest') }
    'unsafe-id' = { param($c) $c.characters[0].id = '../Elsewhere' }
    'invalid-wrist' = { param($c) $c.characters[0].leftWrist = @(321,20) }
    'invalid-accent' = { param($c) $c.characters[0].accent = 'red' }
    'missing-image' = { param($c) $c.characters[0].id = 'MissingCharacter' }
}
$passed = 0
foreach ($test in $mutations.GetEnumerator()) {
    $catalog = $catalogText | ConvertFrom-Json
    & $test.Value $catalog
    $file = Join-Path $scratch ($test.Key + '.json')
    $catalog | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $file -Encoding UTF8
    $rejected = $false
    try { $null = Test-CharacterAssets $file } catch { $rejected = $true }
    if (-not $rejected) { throw "Invalid catalog was accepted: $($test.Key)" }
    $passed++
}
Write-Output ("PASS: {0} invalid asset catalogs rejected." -f $passed)

$ErrorActionPreference = 'Stop'
$script:LabPalsRoot = Split-Path -Parent $PSScriptRoot

function Get-ProjectPath {
    param([Parameter(Mandatory=$true)][string]$Path)
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    return [IO.Path]::GetFullPath((Join-Path $script:LabPalsRoot $Path))
}

function Get-AppVersion {
    $version = (Get-Content -LiteralPath (Get-ProjectPath 'VERSION') -Raw -Encoding UTF8).Trim()
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must contain major.minor.patch.' }
    foreach ($part in $version.Split('.')) { if ([int]$part -gt 65534) { throw 'VERSION component is too large.' } }
    return $version
}

function Get-CharacterCatalog {
    param([string]$CatalogPath = (Get-ProjectPath 'assets/characters.json'))
    return (Get-Content -LiteralPath $CatalogPath -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Test-CharacterAssets {
    param([string]$CatalogPath = (Get-ProjectPath 'assets/characters.json'))
    $catalog = Get-CharacterCatalog $CatalogPath
    if ($catalog.schemaVersion -ne 1) { throw 'Unsupported character catalog schema.' }
    if ($catalog.canvas.width -ne 640 -or $catalog.canvas.height -ne 560) { throw 'Character canvas must be 640 x 560.' }
    $requiredLayers = @('', '-rest', '-left', '-right', '-reaction', '-sleep')
    if (@($catalog.layers).Count -ne 6 -or @(Compare-Object $requiredLayers @($catalog.layers)).Count -ne 0) {
        throw 'Catalog must declare all six supported character layers.'
    }
    $characters = @($catalog.characters)
    if ($characters.Count -eq 0) { throw 'Character catalog is empty.' }
    $ids = @{}; $values = @{}; $files = @{}
    Add-Type -AssemblyName System.Drawing
    foreach ($item in $characters) {
        if ($item.id -cnotmatch '^[A-Z][A-Za-z0-9]*$' -or $ids.ContainsKey($item.id)) { throw "Invalid or duplicate character id: $($item.id)" }
        if ($null -eq $item.value -or [double]$item.value -ne [int]$item.value -or $item.value -lt 0 -or $values.ContainsKey([string]$item.value)) { throw "Invalid or duplicate enum value: $($item.id)" }
        $ids[$item.id] = $true; $values[[string]$item.value] = $true
        foreach ($property in @('name','shortName','english','traits','reactionCaption')) {
            if ([string]::IsNullOrWhiteSpace([string]$item.$property)) { throw "Missing $property for $($item.id)" }
        }
        if ($item.accent -notmatch '^#[0-9a-fA-F]{6}$') { throw "Invalid accent: $($item.id)" }
        foreach ($side in @('leftWrist','rightWrist')) {
            $point = @($item.$side)
            if ($point.Count -ne 2) { throw "Invalid $side for $($item.id)" }
            for ($axis = 0; $axis -lt 2; $axis++) {
                $coordinate = [double]$point[$axis]
                $limit = @(320,280)[$axis]
                if ([double]::IsNaN($coordinate) -or [double]::IsInfinity($coordinate) -or $coordinate -lt 0 -or $coordinate -gt $limit) {
                    throw "Out-of-bounds $side for $($item.id)"
                }
            }
        }
        foreach ($suffix in $requiredLayers) {
            $name = "$($item.id)$suffix.png"
            $file = Get-ProjectPath "assets/characters/$name"
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing runtime layer: $name" }
            $image = [Drawing.Bitmap]::FromFile($file)
            try {
                if ($image.Width -ne 640 -or $image.Height -ne 560) { throw "Wrong dimensions: $name" }
                foreach ($corner in @(@(0,0),@(639,0),@(0,559),@(639,559))) {
                    if ($image.GetPixel($corner[0],$corner[1]).A -ne 0) { throw "Opaque canvas corner: $name" }
                }
            } finally { $image.Dispose() }
            $files[$name] = $true
        }
    }
    foreach ($file in Get-ChildItem -LiteralPath (Get-ProjectPath 'assets/characters') -File) {
        if (-not $files.ContainsKey($file.Name)) { throw "Unregistered runtime asset: $($file.Name)" }
    }
    return $catalog
}

function ConvertTo-CSharpString {
    param([string]$Value)
    return '"' + $Value.Replace('\','\\').Replace('"','\"').Replace("`r",'\r').Replace("`n",'\n') + '"'
}

function Invoke-Diagnostic {
    param([string]$Executable, [string]$Mode, [string]$Destination)
    $destinationPath = Get-ProjectPath $Destination
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($destinationPath)) | Out-Null
    $errorFile = $destinationPath + '.error.txt'
    if (Test-Path -LiteralPath $errorFile) { Remove-Item -LiteralPath $errorFile }
    $process = Start-Process -FilePath $Executable -ArgumentList @($Mode, ('"' + $destinationPath + '"')) -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $errorFile) { throw (Get-Content -LiteralPath $errorFile -Raw -Encoding UTF8) }
        throw "Diagnostic $Mode exited with code $($process.ExitCode)."
    }
}

<#
.SYNOPSIS
    Produit l'installeur EXE de TraceZero (Phase 19) via Inno Setup.

.DESCRIPTION
    Publie TraceZero.App et TraceZero.Elevated en self-contained win-x64 dans un dossier de staging
    (SANS le marqueur portable — l'app installée range ses données dans %LOCALAPPDATA%\TraceZero),
    puis compile build\installer\TraceZero.iss avec Inno Setup pour produire
    artifacts\installer\TraceZeroSetup-<version>.exe.

    L'installeur est PER-USER par défaut (aucun admin requis) ; l'utilisateur peut choisir une
    installation pour tous (Program Files) dans l'assistant. La signature de code reste à appliquer
    (certificat requis) — voir docs\distribution-strategy.md.

.PARAMETER Configuration
    Configuration de build (défaut : Release).
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')
$Stage = Join-Path $RepoRoot 'artifacts\installer\stage'
$OutDir = Join-Path $RepoRoot 'artifacts\installer'
$Iss = Join-Path $RepoRoot 'build\installer\TraceZero.iss'

# Version depuis Directory.Build.props.
$propsText = Get-Content (Join-Path $RepoRoot 'Directory.Build.props') -Raw
if ($propsText -notmatch '<Version>\s*([^<]+?)\s*</Version>') { throw 'Version introuvable dans Directory.Build.props.' }
$Version = $Matches[1].Trim()

# Localiser ISCC (Inno Setup 6).
$Iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Iscc) { throw "Inno Setup 6 introuvable. Installez-le : winget install JRSoftware.InnoSetup" }

Push-Location $RepoRoot
try {
    if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
    New-Item -ItemType Directory -Path $Stage -Force | Out-Null

    Write-Host "== Publish TraceZero.App (installé, self-contained) ==" -ForegroundColor Cyan
    dotnet publish (Join-Path $RepoRoot 'src\TraceZero.App\TraceZero.App.csproj') `
        -c $Configuration -r $Runtime --self-contained true -o $Stage
    if ($LASTEXITCODE -ne 0) { throw 'Échec publish App.' }

    Write-Host "== Publish TraceZero.Elevated (helper) ==" -ForegroundColor Cyan
    dotnet publish (Join-Path $RepoRoot 'src\TraceZero.Elevated\TraceZero.Elevated.csproj') `
        -c $Configuration -r $Runtime --self-contained true -o $Stage
    if ($LASTEXITCODE -ne 0) { throw 'Échec publish Elevated.' }

    # Pas de marqueur portable : l'app installée utilise %LOCALAPPDATA%\TraceZero.
    $marker = Join-Path $Stage 'tracezero.portable'
    if (Test-Path $marker) { Remove-Item $marker -Force }

    Write-Host "== Compilation de l'installeur (Inno Setup) ==" -ForegroundColor Cyan
    & $Iscc "/DMyAppVersion=$Version" "/DStageDir=$Stage" $Iss
    if ($LASTEXITCODE -ne 0) { throw 'Échec de la compilation Inno Setup.' }

    $setup = Join-Path $OutDir "TraceZeroSetup-$Version.exe"
    Write-Host ''
    Write-Host "Installeur prêt : $setup" -ForegroundColor Green
    if (Test-Path $setup) {
        $mb = [math]::Round((Get-Item $setup).Length / 1MB, 1)
        Write-Host "  Taille : $mb Mo"
        $hash = (Get-FileHash $setup -Algorithm SHA256).Hash
        Write-Host "  SHA-256 : $hash"
    }
    Write-Host '  [ ] À FAIRE avant distribution : signer l''installeur + les exe (certificat + timestamp).' -ForegroundColor Yellow
}
finally {
    Pop-Location
}

<#
.SYNOPSIS
    Produit une build Portable de TraceZero (Phase 19, §29).

.DESCRIPTION
    Publie TraceZero.App et TraceZero.Elevated en self-contained win-x64 dans un dossier unique,
    dépose le marqueur portable (« tracezero.portable ») pour que l'application stocke ses données
    dans <dossier>\Data (aucune écriture cachée ailleurs), puis empaquette le tout en .zip.

    Aucune installation, aucune écriture en dehors du dossier portable. La signature de code
    (app, updater, helper elevated) reste à appliquer en production (certificat requis) — voir
    KNOWN_LIMITATIONS.md.

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
$OutDir = Join-Path $RepoRoot 'artifacts\portable\TraceZero'
$ZipPath = Join-Path $RepoRoot 'artifacts\portable\TraceZero-portable.zip'

Push-Location $RepoRoot
try {
    if (Test-Path (Split-Path $OutDir)) { Remove-Item (Split-Path $OutDir) -Recurse -Force }
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

    Write-Host '== Publish TraceZero.App (portable, self-contained) ==' -ForegroundColor Cyan
    dotnet publish (Join-Path $RepoRoot 'src/TraceZero.App/TraceZero.App.csproj') `
        -c $Configuration -r $Runtime --self-contained true -o $OutDir
    if ($LASTEXITCODE -ne 0) { throw 'Échec publish App.' }

    Write-Host '== Publish TraceZero.Elevated (helper) ==' -ForegroundColor Cyan
    dotnet publish (Join-Path $RepoRoot 'src/TraceZero.Elevated/TraceZero.Elevated.csproj') `
        -c $Configuration -r $Runtime --self-contained true -o $OutDir
    if ($LASTEXITCODE -ne 0) { throw 'Échec publish Elevated.' }

    # Marqueur portable : active le stockage des données à côté de l'exe.
    Set-Content -Path (Join-Path $OutDir 'tracezero.portable') -Value 'portable' -Encoding utf8

    Write-Host '== Empaquetage .zip ==' -ForegroundColor Cyan
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    Compress-Archive -Path (Join-Path $OutDir '*') -DestinationPath $ZipPath

    Write-Host ''
    Write-Host "Build portable prête : $ZipPath" -ForegroundColor Green
    Write-Host '  [ ] À FAIRE avant distribution : signer app + updater + helper elevated (certificat + timestamp).' -ForegroundColor Yellow
}
finally {
    Pop-Location
}

<#
.SYNOPSIS
    Pipeline de qualité release pour TraceZero (Phase 27, §37).

.DESCRIPTION
    Exécute les portes de qualité *automatisables* et échoue si l'une d'elles échoue :
      - restore
      - build -c Release  (échec si le moindre avertissement : objectif « Release 0 warning »)
      - test  -c Release  (tests unitaires, sécurité et intégration)
      - publish (win-x64) de TraceZero.App et TraceZero.Elevated
      - empreinte SHA-256 des binaires publiés

    Les portes nécessitant des ressources externes (certificat de signature, antivirus,
    machine virtuelle) ne sont JAMAIS simulées : elles sont listées honnêtement comme
    « à effectuer manuellement » avant expédition. Voir KNOWN_LIMITATIONS.md.

.PARAMETER Configuration
    Configuration de build (défaut : Release).

.PARAMETER Runtime
    RID de publication (défaut : win-x64).

.EXAMPLE
    pwsh build/scripts/release.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Racine du dépôt = deux niveaux au-dessus de ce script (compatible PS 5.1 et 7).
$RepoRoot = Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')
$Solution = Join-Path $RepoRoot 'TraceZero.slnx'
$ArtifactsDir = Join-Path $RepoRoot 'artifacts'

function Write-Step([string]$Message) {
    Write-Host ''
    Write-Host "==== $Message ====" -ForegroundColor Cyan
}

function Invoke-Checked([string]$What, [scriptblock]$Action) {
    Write-Step $What
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Échec : $What (code $LASTEXITCODE)."
    }
}

Push-Location $RepoRoot
try {
    Invoke-Checked 'Restore' { dotnet restore $Solution }

    # Build Release : on capture la sortie pour faire échouer sur tout avertissement.
    Write-Step "Build -c $Configuration (0 avertissement requis)"
    $buildLog = dotnet build $Solution -c $Configuration --no-restore 2>&1
    $buildLog | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { throw 'Échec : build.' }
    $warningLine = $buildLog | Select-String -Pattern '(\d+)\s+Avertissement|(\d+)\s+Warning' | Select-Object -Last 1
    if ($warningLine) {
        $count = ([regex]'(\d+)').Match($warningLine.ToString()).Value
        if ([int]$count -ne 0) {
            throw "Release exige 0 avertissement ; $count trouvé(s)."
        }
    }

    Invoke-Checked "Test -c $Configuration (unitaires + sécurité + intégration)" {
        dotnet test $Solution -c $Configuration --no-build
    }

    # Publication des exécutables signables.
    if (Test-Path $ArtifactsDir) { Remove-Item $ArtifactsDir -Recurse -Force }
    New-Item -ItemType Directory -Path $ArtifactsDir | Out-Null

    foreach ($proj in @('src/TraceZero.App/TraceZero.App.csproj', 'src/TraceZero.Elevated/TraceZero.Elevated.csproj')) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($proj)
        Invoke-Checked "Publish $name ($Runtime)" {
            dotnet publish (Join-Path $RepoRoot $proj) -c $Configuration -r $Runtime `
                --self-contained false -o (Join-Path $ArtifactsDir $name)
        }
    }

    # Empreintes SHA-256 (§37).
    Write-Step 'Empreintes SHA-256'
    $hashes = Get-ChildItem $ArtifactsDir -Recurse -Filter '*.exe' | ForEach-Object {
        $h = Get-FileHash $_.FullName -Algorithm SHA256
        "{0}  {1}" -f $h.Hash, $_.Name
    }
    $hashes | ForEach-Object { Write-Host $_ }
    $hashes | Set-Content -Path (Join-Path $ArtifactsDir 'SHA256SUMS.txt') -Encoding utf8

    Write-Host ''
    Write-Host 'Portes automatisées : OK.' -ForegroundColor Green

    # Portes manuelles / dépendantes de ressources externes — jamais simulées (§0, §37).
    Write-Step 'À EFFECTUER MANUELLEMENT avant expédition (non simulé)'
    @(
        'Signature Authenticode des .exe (certificat EV requis) puis vérification (signtool verify /pa).',
        'Scan antivirus des artefacts (Defender/VirusTotal).',
        'Smoke test installation puis désinstallation sur une machine propre.',
        'Test de l''updater signé (Phase 18).',
        'Test de nettoyage réel en VM (jamais sur le disque du développeur — §19).'
    ) | ForEach-Object { Write-Host "  [ ] $_" -ForegroundColor Yellow }
}
finally {
    Pop-Location
}

<#
.SYNOPSIS
    Génère src\TraceZero.App\Assets\logo.ico (multi-résolutions) depuis logo.png.

.DESCRIPTION
    Centre le logo (non carré) sur un canevas carré transparent, puis produit un .ico contenant
    les tailles 256/128/64/48/32/16 (entrées PNG, supportées par Windows Vista+). Utilise System.Drawing
    (présent dans Windows PowerShell 5.1). À relancer si le logo change.
#>
[CmdletBinding()]
param(
    [string]$Source = 'src\TraceZero.App\Assets\logo.png',
    [string]$Output = 'src\TraceZero.App\Assets\logo.ico'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$RepoRoot = Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')
$srcPath = Join-Path $RepoRoot $Source
$outPath = Join-Path $RepoRoot $Output

$src = [System.Drawing.Image]::FromFile($srcPath)
try {
    # Canevas carré transparent, logo centré (évite toute déformation).
    $side = [Math]::Max($src.Width, $src.Height)
    $square = New-Object System.Drawing.Bitmap($side, $side)
    $g = [System.Drawing.Graphics]::FromImage($square)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, [int](($side - $src.Width) / 2), [int](($side - $src.Height) / 2), $src.Width, $src.Height)
    $g.Dispose()

    $sizes = 256, 128, 64, 48, 32, 16
    $pngs = @()
    foreach ($s in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($s, $s)
        $gg = [System.Drawing.Graphics]::FromImage($bmp)
        $gg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $gg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $gg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $gg.Clear([System.Drawing.Color]::Transparent)
        $gg.DrawImage($square, 0, 0, $s, $s)
        $gg.Dispose()
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngs += , ($ms.ToArray())
        $bmp.Dispose(); $ms.Dispose()
    }
    $square.Dispose()

    # Conteneur ICO : en-tête + répertoire + blobs PNG.
    $fs = [System.IO.File]::Create($outPath)
    $bw = New-Object System.IO.BinaryWriter($fs)
    $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)  # reserved, type=icon, count
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $s = $sizes[$i]; $data = $pngs[$i]
        $dim = if ($s -ge 256) { 0 } else { $s }   # 0 == 256 dans le format ICO
        $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)  # w,h,colors,reserved
        $bw.Write([UInt16]1); $bw.Write([UInt16]32)                # planes, bpp
        $bw.Write([UInt32]$data.Length); $bw.Write([UInt32]$offset)
        $offset += $data.Length
    }
    foreach ($data in $pngs) { $bw.Write($data) }
    $bw.Flush(); $bw.Close(); $fs.Close()
}
finally {
    $src.Dispose()
}

Write-Host "Icône générée : $outPath" -ForegroundColor Green
Write-Host ("  Tailles : " + ($sizes -join ', ') + "  |  " + [Math]::Round((Get-Item $outPath).Length / 1KB, 1) + " Ko")

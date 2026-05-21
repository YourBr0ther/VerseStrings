<#
.SYNOPSIS
  Renders the VerseStrings icon to a multi-resolution .ico file.

.DESCRIPTION
  Three concentric blue rings around a gold 5-point star.
  Mirrors art/icon.svg — the SVG is the visual reference; this script
  is the build step. Pure System.Drawing, no extra tooling required.

.EXAMPLE
  pwsh art\Build-Icon.ps1 -OutputPath src\VerseStrings.App\Assets\icon.ico
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

Add-Type -AssemblyName System.Drawing

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256

function New-IconBitmap {
    param([int] $Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $Size / 256.0
    $center = $Size / 2.0
    $ringRgb   = [System.Drawing.Color]::FromArgb(0x4A, 0x90, 0xE2)
    $starColor = [System.Drawing.Color]::FromArgb(0xF5, 0xC8, 0x42)

    Draw-Ring $g $center (108 * $scale) ([Math]::Max(1.0, 10 * $scale)) $ringRgb 0.45
    Draw-Ring $g $center  (85 * $scale) ([Math]::Max(1.0, 12 * $scale)) $ringRgb 0.72
    Draw-Ring $g $center  (65 * $scale) ([Math]::Max(1.0, 14 * $scale)) $ringRgb 1.00
    Draw-Star $g $center  (45 * $scale)                    (17 * $scale) $starColor

    $g.Dispose()
    return $bmp
}

function Draw-Ring {
    param($Graphics, $Center, $Radius, $Stroke, $Color, $Opacity)
    $alpha = [byte]($Opacity * 255)
    $argb  = [System.Drawing.Color]::FromArgb($alpha, $Color.R, $Color.G, $Color.B)
    $pen   = New-Object System.Drawing.Pen($argb, [single]$Stroke)
    $Graphics.DrawEllipse($pen, [single]($Center - $Radius), [single]($Center - $Radius), [single]($Radius * 2), [single]($Radius * 2))
    $pen.Dispose()
}

function Draw-Star {
    param($Graphics, $Center, $OuterR, $InnerR, $Color)
    $points = New-Object 'System.Drawing.PointF[]' 10
    for ($i = 0; $i -lt 10; $i++) {
        if (($i % 2) -eq 0) { $r = $OuterR } else { $r = $InnerR }
        $angle = (-[Math]::PI / 2) + $i * [Math]::PI / 5
        $x = [single]($Center + $r * [Math]::Cos($angle))
        $y = [single]($Center + $r * [Math]::Sin($angle))
        $points[$i] = New-Object System.Drawing.PointF($x, $y)
    }
    $brush = New-Object System.Drawing.SolidBrush($Color)
    $Graphics.FillPolygon($brush, $points)
    $brush.Dispose()
}

# Render each size to a PNG byte array.
$pngs = @()
foreach ($sz in $sizes) {
    $bmp = New-IconBitmap -Size $sz
    $ms  = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngs += , $ms.ToArray()
}

# Pack into multi-resolution ICO.
$dir = Split-Path -Parent $OutputPath
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
$fs = [System.IO.File]::Create($OutputPath)
$bw = New-Object System.IO.BinaryWriter($fs)
try {
    $bw.Write([uint16] 0)              # reserved
    $bw.Write([uint16] 1)              # type = ICO
    $bw.Write([uint16] $sizes.Count)   # image count

    $offset = 6 + $sizes.Count * 16
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $sz = $sizes[$i]
        if ($sz -ge 256) { $w = 0 } else { $w = $sz }
        $bw.Write([byte] $w)              # width
        $bw.Write([byte] $w)              # height
        $bw.Write([byte] 0)               # color count
        $bw.Write([byte] 0)               # reserved
        $bw.Write([uint16] 1)             # planes
        $bw.Write([uint16] 32)            # bits per pixel
        $bw.Write([uint32] $pngs[$i].Length)
        $bw.Write([uint32] $offset)
        $offset += $pngs[$i].Length
    }
    foreach ($png in $pngs) { $bw.Write($png) }
}
finally {
    $bw.Dispose()
    $fs.Dispose()
}

Write-Host "Wrote $OutputPath ($((Get-Item $OutputPath).Length) bytes, $($sizes.Count) sizes)"

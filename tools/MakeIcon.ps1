$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$target = Join-Path $PSScriptRoot '..\assets\app.ico'
$frames = @()
foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(150, 180, 128))
    $unit = $size / 32.0
    $g.ScaleTransform($unit, $unit)
    $road = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(64, 79, 80))
    $stripe = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(244, 241, 223))
    $accent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(218, 135, 110))
    $g.FillRectangle($road, 0, 7, 32, 18)
    foreach ($y in @(9, 14, 19)) { $g.FillRectangle($stripe, 12, $y, 8, 3) }
    $g.FillRectangle($accent, 13, 24, 6, 5)
    $g.FillRectangle($stripe, 14, 21, 4, 4)
    $g.Dispose(); $road.Dispose(); $stripe.Dispose(); $accent.Dispose()
    $stream = New-Object System.IO.MemoryStream
    $bmp.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames += ,@($size, $stream.ToArray())
    $stream.Dispose(); $bmp.Dispose()
}
$file = [System.IO.File]::Create($target)
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
    $dimension = if ($frame[0] -eq 256) { 0 } else { $frame[0] }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$frame[1].Length); $writer.Write([uint32]$offset)
    $offset += $frame[1].Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame[1]) }
$writer.Dispose(); $file.Dispose()
Write-Output 'Created seven-size game icon.'

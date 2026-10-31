# Original code-native FocGrain mark. No vendor logos or font assets.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Force $assets | Out-Null
function Draw-Mark([string]$mode) {
    $bitmap = [Drawing.Bitmap]::new(256,256)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([Drawing.Color]::Transparent)
    $path = [Drawing.Drawing2D.GraphicsPath]::new()
    foreach ($arc in @(@(8,8,116,116,180),@(132,8,116,116,270),@(132,132,116,116,0),@(8,132,116,116,90))) {
        $path.AddArc([single]$arc[0],[single]$arc[1],[single]$arc[2],[single]$arc[3],[single]$arc[4],90)
    }
    $path.CloseFigure()
    $brush = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(16,16),[Drawing.Point]::new(232,240),[Drawing.ColorTranslator]::FromHtml('#1c2d47'),[Drawing.ColorTranslator]::FromHtml('#0b1425'))
    $g.FillPath($brush,$path); $brush.Dispose()
    $pen = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#314663'),3)
    $g.DrawPath($pen,$path); $pen.Dispose(); $path.Dispose()
    $pen = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#34445e'),15)
    $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen,52,46,152,152,135,270); $pen.Dispose()
    $angle = switch ($mode) { 'min' {135} 'max' {405} 'mid' {270} default {315} }
    $color = switch ($mode) { 'min' {'#92979f'} 'max' {'#ff850a'} 'error' {'#ff354d'} 'unknown' {'#8ba2bc'} default {'#39dfc4'} }
    $pen = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml($color),15)
    $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen,52,46,152,152,135,[single][Math]::Max(1,($angle-135))); $pen.Dispose()
    $fill = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#1a2d46'))
    $g.FillEllipse($fill,80,74,96,96); $fill.Dispose()
    $pen = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#425c79'),3)
    $g.DrawEllipse($pen,80,74,96,96); $pen.Dispose()
    $pointerColor = if ($mode -eq 'min') { '#92979f' } else { '#f1f9ff' }
    $pen = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml($pointerColor),10)
    $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $radians = $angle * [Math]::PI / 180
    $g.DrawLine($pen,128,122,[single](128+41*[Math]::Cos($radians)),[single](122+41*[Math]::Sin($radians))); $pen.Dispose()
    foreach ($segment in @(@(65,'#39dfc4'),@(139,'#ffbd69'))) {
        $segmentColor = if ($mode -in @('min','max')) { $color } else { $segment[1] }
        $pen = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml($segmentColor),13)
        $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
        $g.DrawLine($pen,[single]($segment[0]+6),213,[single]($segment[0]+46),213);$pen.Dispose()
    }
    if ($mode -ne 'app') {
        $badgeColor = if ($mode -eq 'error') { '#0b1425' } else { $color }
        $fill = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($badgeColor))
        $g.FillEllipse($fill,164,166,74,74);$fill.Dispose()
        $text = switch ($mode) { 'min' {''} 'max' {'+'} 'error' {'!'} 'unknown' {'?'} default {'•'} }
        $fontSize = if ($mode -eq 'error') { 68 } else { 43 }
        $font = [Drawing.Font]::new('Segoe UI',$fontSize,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
        $format=[Drawing.StringFormat]::new();$format.Alignment=$format.LineAlignment=[Drawing.StringAlignment]::Center
        $textColor = if ($mode -eq 'error') { $color } else { '#0b1425' }
        $fill=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($textColor))
        $g.DrawString($text,$font,$fill,[Drawing.RectangleF]::new(164,162,74,78),$format)
        $fill.Dispose();$font.Dispose();$format.Dispose()
    }
    $g.Dispose()
    return ,$bitmap
}
foreach ($mode in @('app','min','max','mid','unknown','error')) {
    $bitmap = Draw-Mark $mode
    $name = if ($mode -eq 'app') {'FocGrain'} else {"FocGrain-$mode"}
    if ($mode -eq 'app') {$bitmap.Save((Join-Path $assets 'FocGrain.png'),[Drawing.Imaging.ImageFormat]::Png)}
    $images = @()
    foreach ($size in @(16,20,24,32,40,48,64,128,256)) {
        $resized = [Drawing.Bitmap]::new($size,$size)
        $g = [Drawing.Graphics]::FromImage($resized)
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($bitmap,0,0,$size,$size); $g.Dispose()
        $stream = [IO.MemoryStream]::new(); $resized.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
        $images += ,@($size,$stream.ToArray()); $stream.Dispose();$resized.Dispose()
    }
    $stream = [IO.File]::Create((Join-Path $assets "$name.ico"));$writer=[IO.BinaryWriter]::new($stream)
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$images.Count)
    $offset = 6+16*$images.Count
    foreach ($entry in $images) {
        $n = if ($entry[0] -eq 256) {0} else {$entry[0]}
        $writer.Write([byte]$n);$writer.Write([byte]$n);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$entry[1].Length);$writer.Write([uint32]$offset)
        $offset += $entry[1].Length
    }
    foreach ($entry in $images) {$writer.Write([byte[]]$entry[1])}
    $writer.Dispose();$bitmap.Dispose()
}
$banner = [Drawing.Bitmap]::new(410,797)
$g = [Drawing.Graphics]::FromImage($banner)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([Drawing.ColorTranslator]::FromHtml('#0b1425'))
$mark = [Drawing.Image]::FromFile((Join-Path $assets 'FocGrain.png'))
$g.DrawImage($mark,77,174,256,256); $mark.Dispose()
$font = [Drawing.Font]::new('Segoe UI',42,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$format = [Drawing.StringFormat]::new(); $format.Alignment=[Drawing.StringAlignment]::Center
$g.DrawString('FocGrain',$font,[Drawing.Brushes]::White,[Drawing.RectangleF]::new(0,475,410,70),$format)
$font.Dispose()
$font = [Drawing.Font]::new('Segoe UI',18,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)
$brush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#8ba2bc'))
$g.DrawString('MIN  ↔  MAX',$font,$brush,[Drawing.RectangleF]::new(0,545,410,40),$format)
$font.Dispose();$brush.Dispose();$format.Dispose();$g.Dispose()
$banner.Save((Join-Path $assets 'InstallerBanner.png'),[Drawing.Imaging.ImageFormat]::Png);$banner.Dispose()

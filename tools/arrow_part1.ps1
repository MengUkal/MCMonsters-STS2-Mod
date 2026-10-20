$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)

# ===== 1. 图集辅助函数（贴箭矢到图集）=====
$helper = @'

// ---- 把箭矢贴入图集并扩展画布 ----
static void AddArrowToAtlasPng(string atlasPngPath, string arrowPath, int px, int py)
{
    using var atlas = new System.Drawing.Bitmap(atlasPngPath);
    using var arrow = new System.Drawing.Bitmap(arrowPath);
    using var canvas = new System.Drawing.Bitmap(Math.Max(atlas.Width, px + arrow.Width), Math.Max(atlas.Height, py + arrow.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    using (var g = System.Drawing.Graphics.FromImage(canvas))
    {
        g.DrawImageUnscaled(atlas, 0, 0);
        g.DrawImageUnscaled(arrow, px, py);
    }
    canvas.Save(atlasPngPath, System.Drawing.Imaging.ImageFormat.Png);
}

static void ComposeTwoFrameAtlasWithArrow(string baseFramePath, string arrowPath, string outPath)
{
    ComposeTwoFrameAtlas(baseFramePath, outPath);
    AddArrowToAtlasPng(outPath, arrowPath, 600, 100);
}
'@
$anchor1 = 'static void ComposeTwoFrameAtlas(string baseFramePath, string outPath)'
if (-not $c.Contains('AddArrowToAtlasPng')) {
  $c = $c.Replace($anchor1, $helper + "`r`n" + $anchor1)
  Write-Host '1. helper added'
} else { Write-Host '1. helper already present' }

# ===== 2. 骷髅：贴箭矢到(300,500) =====
$old2 = 'ComposeSkeletonFrames(skelBaseFrame, composedAtlasPng);'
$new2 = 'ComposeSkeletonFrames(skelBaseFrame, composedAtlasPng);' + "`r`n" + 'AddArrowToAtlasPng(composedAtlasPng, Path.Combine(skelDir, "arrow_source.png"), 300, 500);'
if ($c.Contains('AddArrowToAtlasPng(composedAtlasPng')) { Write-Host '2. skeleton arrow already present' } else {
  $c = $c.Replace($old2, $new2)
  Write-Host '2. skeleton arrow compose added'
}

# ===== 3. 骷髅图集文本：加arrow区域 =====
$old3 = "full_draw`nbounds:0,500,300,500`n`"`"`";"
$new3 = "full_draw`nbounds:0,500,300,500`narrow`nbounds:300,500,300,300`n`"`"`";"
if ($c.Contains("arrow`nbounds:300,500,300,300")) { Write-Host '3. skeleton atlas region already present' } else {
  $c = $c.Replace($old3, $new3)
  Write-Host '3. skeleton atlas region added'
}

# ===== 4. 掠夺者：合成带箭矢 + 图集900宽 + arrow区域 =====
$old4 = 'ComposeTwoFrameAtlas(pillagerBaseFrame, pillagerAtlasPng);'
$new4 = 'ComposeTwoFrameAtlasWithArrow(pillagerBaseFrame, Path.Combine(pillagerDir, "arrow_source.png"), pillagerAtlasPng);'
if ($c.Contains('ComposeTwoFrameAtlasWithArrow(pillagerBaseFrame')) { Write-Host '4a. pillager compose already present' } else {
  $c = $c.Replace($old4, $new4)
  Write-Host '4a. pillager compose swapped'
}
$c = $c.Replace("pillager.png`nsize:600,500", "pillager.png`nsize:900,500")

# pillager图集块加arrow区域（scoped）
$atlasStart = $c.IndexOf('var pillagerAtlas =')
$atlasEnd = $c.IndexOf('File.WriteAllText', $atlasStart)
$scope = $c.Substring($atlasStart, $atlasEnd - $atlasStart)
if (-not $scope.Contains('bounds:600,100,300,300')) {
  $scopeNew = $scope.Replace("full_flash`n" + 'bounds:300,0,300,500', "full_flash`n" + 'bounds:300,0,300,500' + "`n" + "arrow`n" + 'bounds:600,100,300,300')
  $c = $c.Substring(0, $atlasStart) + $scopeNew + $c.Substring($atlasEnd)
  Write-Host '4c. pillager atlas region added'
} else { Write-Host '4c. already present' }

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'part 1 done'

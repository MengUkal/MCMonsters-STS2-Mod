$ErrorActionPreference = 'Stop'
$NL = [char]10
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)

# ===== 1. 添加三帧图集辅助函数（开壳+闪白+闭壳）=====
$helper = @'

// ---- 潜影贝三帧图集：开壳(0) + 白闪(300) + 闭壳(600) ----
static void ComposeShulkerAtlas(string openFramePath, string closedSourcePath, string outPath)
{
    using var openImg = new System.Drawing.Bitmap(openFramePath);
    using var closedSrc = new System.Drawing.Bitmap(closedSourcePath);
    using var canvas = new System.Drawing.Bitmap(900, 500, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    using (var g = System.Drawing.Graphics.FromImage(canvas))
    {
        // 开壳帧
        g.DrawImageUnscaled(openImg, 0, 0);
        // 白闪帧（开壳剪影）
        using var flash = (System.Drawing.Bitmap)openImg.Clone();
        for (int y = 0; y < flash.Height; y++)
        for (int x = 0; x < flash.Width; x++)
        {
            var px = flash.GetPixel(x, y);
            if (px.A > 40) flash.SetPixel(x, y, System.Drawing.Color.FromArgb(px.A, 255, 255, 255));
        }
        g.DrawImageUnscaled(flash, 300, 0);
        // 闭壳帧：等比适配300x500
        double sc = Math.Min(300.0 / closedSrc.Width, 495.0 / closedSrc.Height);
        int dw = Math.Max(1, (int)(closedSrc.Width * sc));
        int dh = Math.Max(1, (int)(closedSrc.Height * sc));
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(closedSrc, 600 + (300 - dw) / 2, 500 - 5 - dh, dw, dh);
    }
    canvas.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
}
'@
if (-not $c.Contains('ComposeShulkerAtlas')) {
  $anchor = 'static void ComposeTwoFrameAtlas(string baseFramePath, string outPath)'
  $c = $c.Replace($anchor, $helper + $NL + $anchor)
  Write-Host '1. ComposeShulkerAtlas added'
} else { Write-Host '1. already present' }

# ===== 2. 潜影贝段落改用三帧图集 + 900宽 atlas =====
$oldCompose = 'ComposeTwoFrameAtlas(shulkerBaseFrame, shulkerAtlasPng);'
$newCompose = 'ComposeShulkerAtlas(shulkerBaseFrame, Path.Combine(shulkerDir, "shulker_closed_source.png"), shulkerAtlasPng);'
if (-not $c.Contains('ComposeShulkerAtlas(shulkerBaseFrame')) { $c = $c.Replace($oldCompose, $newCompose); Write-Host '2. shulker compose swapped' } else { Write-Host '2. already swapped' }
$c = $c.Replace("shulker.png`n" + "size:600,500", "shulker.png`n" + "size:900,500")
# atlas文本加闭壳区域
$as = $c.IndexOf('var shulkerAtlas =')
$ae = $c.IndexOf('File.WriteAllText', $as)
$scope = $c.Substring($as, $ae - $as)
if (-not $scope.Contains('full_closed')) {
  $scopeNew = $scope.Replace("full_flash`n" + 'bounds:300,0,300,500', "full_flash`n" + 'bounds:300,0,300,500' + "`n" + "full_closed`n" + 'bounds:600,0,300,500')
  $c = $c.Substring(0, $as) + $scopeNew + $c.Substring($ae)
  Write-Host '3. shulker closed region added'
} else { Write-Host '3. already present' }

# ===== 3. 潜影贝JSON：加闭壳附件 + 交换动画 =====
$sj = $c.IndexOf('var shulkerSkel = new JsonObject')
$se = $c.IndexOf('"animations"] = shulkerAnims', $sj)
$jscope = $c.Substring($sj, $se - $sj)
if (-not $jscope.Contains('full_closed')) {
  # 皮肤加full_closed
  $oldSkins = '                ["full"] = new JsonObject' + $NL + '                {' + $NL + '                    ["full"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    },'
  $newSkins = '                ["full"] = new JsonObject' + $NL + '                {' + $NL + '                    ["full"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    },' + $NL + '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 600, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    },'
  if ($jscope.Contains($oldSkins)) { $jscope = $jscope.Replace($oldSkins, $newSkins); Write-Host '4. shulker closed skin added' } else { Write-Host '4. skin anchor miss' }
  $c = $c.Substring(0, $sj) + $jscope + $c.Substring($se)
} else { Write-Host '4. already present' }

# ===== 4. 潜影贝动画：cast=闭壳（换闭壳帧），attack=开壳（恢复开壳帧）=====
$sa = $c.IndexOf('var shulkerAnims = new JsonObject')
$sae = $c.IndexOf('var shulkerSkel = new JsonObject', $sa)
$ascope = $c.Substring($sa, $sae - $sa)
if (-not $ascope.Contains('full_closed')) {
  # cast动画加slots换闭壳帧
  $oldCast = '    ["cast"] = new JsonObject' + $NL + '    {' + $NL + '        ["bones"] = new JsonObject'
  $newCast = '    ["cast"] = new JsonObject' + $NL + '    {' + $NL + '        // 闭壳：换成闭壳贴图（保持到重新开壳）' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["full"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0, ["name"] = "full_closed" })' + $NL + '            }' + $NL + '        },' + $NL + '        ["bones"] = new JsonObject'
  if ($ascope.Contains($oldCast)) { $ascope = $ascope.Replace($oldCast, $newCast); Write-Host '5a. cast closed frame OK' } else { Write-Host '5a. cast anchor miss' }
  # attack动画加slots恢复开壳帧
  $oldAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        ["bones"] = new JsonObject'
  $newAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        // 开壳：恢复开壳贴图' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["full"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0, ["name"] = "full" })' + $NL + '            }' + $NL + '        },' + $NL + '        ["bones"] = new JsonObject'
  if ($ascope.Contains($oldAtk)) { $ascope = $ascope.Replace($oldAtk, $newAtk); Write-Host '5b. attack open frame OK' } else { Write-Host '5b. attack anchor miss' }
  $c = $c.Substring(0, $sa) + $ascope + $c.Substring($sae)
} else { Write-Host '5. already present' }

# ===== 5. 袭击队长视觉场景（掠夺者放大版 targetH 300）=====
$marker = '// ============================ 4. 组装 mod pck ============================'
$captain = @'

// ============================ 1k6. 袭击队长（掠夺者放大版，第3幕精英）===========================
// 复用掠夺者的骨架/图集，只生成更大的视觉场景（targetH 300 vs 掠夺者250）
var captainTscn = BuildCreatureTscn(@"D:\塔2MOD\MCMonsters\pck_content\animations\monsters\pillager\pillager_base.png", "pillager_captain", 300.0);
File.WriteAllText(@"D:\塔2MOD\MCMonsters\pck_content\scenes\creature_visuals\pillager_captain.tscn", captainTscn);
Console.WriteLine("OK scenes/creature_visuals/pillager_captain.tscn（袭击队长·掠夺者放大版）");

'@
if (-not $c.Contains('pillager_captain.tscn')) {
  $c = $c.Replace($marker, $captain + $marker)
  Write-Host '6. captain tscn generator added'
} else { Write-Host '6. already present' }

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'act3 art script done'

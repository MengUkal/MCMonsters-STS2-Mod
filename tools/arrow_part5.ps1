$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$NL = [char]10

# ===== 1. 缩小箭矢：绘制140x140（原来是原尺寸300）=====
$oldDraw = '        using (var canvas = new System.Drawing.Bitmap(Math.Max(atlasCopy.Width, px + arrowCopy.Width), Math.Max(atlasCopy.Height, py + arrowCopy.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb))' + $NL + '        {' + $NL + '            using (var g = System.Drawing.Graphics.FromImage(canvas))' + $NL + '            {' + $NL + '                g.DrawImageUnscaled(atlasCopy, 0, 0);' + $NL + '                g.DrawImageUnscaled(arrowCopy, px, py);' + $NL + '            }' + $NL + '            canvas.Save(atlasPngPath, System.Drawing.Imaging.ImageFormat.Png);' + $NL + '        }'
$newDraw = '        using (var canvas = new System.Drawing.Bitmap(atlasCopy.Width, atlasCopy.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))' + $NL + '        {' + $NL + '            using (var g = System.Drawing.Graphics.FromImage(canvas))' + $NL + '            {' + $NL + '                g.DrawImageUnscaled(atlasCopy, 0, 0);' + $NL + '                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;' + $NL + '                g.DrawImage(arrowCopy, new System.Drawing.Rectangle(px, py, 140, 140));' + $NL + '            }' + $NL + '            canvas.Save(atlasPngPath, System.Drawing.Imaging.ImageFormat.Png);' + $NL + '        }'
if ($c.Contains($oldDraw)) { $c = $c.Replace($oldDraw, $newDraw); Write-Host '1. arrow draw scaled to 140' } else { Write-Host '1. SKIP draw (already patched?)' }

# ===== 2. 图集区域尺寸 300x300 → 140x140 =====
$c = $c.Replace('bounds:600,100,300,300', 'bounds:600,100,140,140')
$c = $c.Replace('bounds:300,500,300,300', 'bounds:300,500,140,140')
Write-Host '2. atlas regions 140'

# ===== 3. JSON附件尺寸 300x300 → 140x140 =====
$c = $c.Replace('["width"] = 300, ["height"] = 300', '["width"] = 140, ["height"] = 140')
Write-Host '3. attachment sizes 140'

# ===== 4. 骷髅攻击：抛物线 + 平滑旋转 =====
$oldSkelTr = '                    new JsonObject { ["time"] = 0.2, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["x"] = -1700, ["y"] = -60 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["angle"] = 140 })'
$newSkelTr = '                    new JsonObject { ["time"] = 0.2, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.3, ["x"] = -430, ["y"] = 45 },' + $NL + '                    new JsonObject { ["time"] = 0.42, ["x"] = -1000, ["y"] = 30 },' + $NL + '                    new JsonObject { ["time"] = 0.58, ["x"] = -1700, ["y"] = -160 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.58, ["angle"] = 158 })'
if ($c.Contains($oldSkelTr)) { $c = $c.Replace($oldSkelTr, $newSkelTr); Write-Host '4. skeleton arc' } else { Write-Host '4. SKIP skeleton arc' }

# 骷髅攻击：arrow插槽加淡出（rgba时间轴）
$oldSkelSlot = '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.22, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.55 })' + $NL + '            }'
$newSkelSlot = '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.6 }),' + $NL + '                ["rgba"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.6, ["color"] = "ffffff00" })' + $NL + '            }'
if ($c.Contains($oldSkelSlot)) { $c = $c.Replace($oldSkelSlot, $newSkelSlot); Write-Host '4b. skeleton fade' } else { Write-Host '4b. SKIP skeleton fade' }

# ===== 5. 掠夺者攻击：抛物线 + 平滑旋转 =====
$oldPillTr = '                    new JsonObject { ["time"] = 0.15, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["x"] = -1700, ["y"] = -60 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["angle"] = 140 })'
$newPillTr = '                    new JsonObject { ["time"] = 0.15, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.26, ["x"] = -430, ["y"] = 45 },' + $NL + '                    new JsonObject { ["time"] = 0.38, ["x"] = -1000, ["y"] = 30 },' + $NL + '                    new JsonObject { ["time"] = 0.58, ["x"] = -1700, ["y"] = -160 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.58, ["angle"] = 158 })'
if ($c.Contains($oldPillTr)) { $c = $c.Replace($oldPillTr, $newPillTr); Write-Host '5. pillager arc' } else { Write-Host '5. SKIP pillager arc' }

# 掠夺者攻击：arrow插槽加淡出
$oldPillSlot = '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.5 })' + $NL + '            }'
$newPillSlot = '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.6 }),' + $NL + '                ["rgba"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.6, ["color"] = "ffffff00" })' + $NL + '            }'
if ($c.Contains($oldPillSlot)) { $c = $c.Replace($oldPillSlot, $newPillSlot); Write-Host '5b. pillager fade' } else { Write-Host '5b. SKIP pillager fade' }

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'part 5 SAVED'

$ErrorActionPreference = 'Stop'
$NL = [char]10
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)

# ===== 1. slots: 加 shell 槽位 =====
$oldSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }' + $NL + '    },'
$newSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },' + $NL + '        new JsonObject { ["name"] = "shell", ["bone"] = "root", ["color"] = "ffffffff" }' + $NL + '    },'
if ($c.Contains($oldSlots)) { $c = $c.Replace($oldSlots, $newSlots); Write-Host '1. shell slot added' } else { Write-Host '1. slot anchor miss' }

# ===== 2. skins: full_closed 移到 shell 槽 =====
# 当前结构：["full"] = { ["full"] = region(0), ["full_closed"] = region(600) }
$oldSkin = '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    },' + $NL + '                }'
$newSkin = '                },' + $NL + '                ["shell"] = new JsonObject' + $NL + '                {' + $NL + '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
if ($c.Contains($oldSkin)) { $c = $c.Replace($oldSkin, $newSkin); Write-Host '2. full_closed moved to shell slot' } else { Write-Host '2. skin anchor miss' }

# ===== 3. cast动画：去掉slots（状态交给closed_loop）=====
$oldCastSlots = '        // 闭壳：换成闭壳贴图（保持到重新开壳）' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["full"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0, ["name"] = "full_closed" })' + $NL + '            }' + $NL + '        },' + $NL
if ($c.Contains($oldCastSlots)) { $c = $c.Replace($oldCastSlots, '        // 闭壳动画只做壳缩紧脉冲，状态由closed_loop接管' + $NL); Write-Host '3. cast slots removed' } else { Write-Host '3. cast anchor miss' }

# ===== 4. attack动画：恢复full槽 + 清空shell槽 =====
$oldAtkSlots = '        // 开壳：恢复开壳贴图' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["full"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0, ["name"] = "full" })' + $NL + '            }' + $NL + '        },'
$newAtkSlots = '        // 开壳：恢复开壳贴图并卸下闭壳' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["full"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0, ["name"] = "full" })' + $NL + '            },' + $NL + '            ["shell"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0, ["name"] = "" })' + $NL + '            }' + $NL + '        },'
if ($c.Contains($oldAtkSlots)) { $c = $c.Replace($oldAtkSlots, $newAtkSlots); Write-Host '4. attack slots updated' } else { Write-Host '4. attack anchor miss' }

# ===== 5. 新增 closed_loop 动画（闭壳待机，循环）=====
$closedLoop = @'
    ["closed_loop"] = new JsonObject
    {
        // 闭壳待机：卸下开壳贴图，挂上闭壳方块，轻微呼吸浮动
        ["slots"] = new JsonObject
        {
            ["full"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "" })
            },
            ["shell"] = new JsonObject
            {
                ["attachment"] = new JsonArray(
                    new JsonObject { ["time"] = 0, ["name"] = "full_closed" })
            }
        },
        ["bones"] = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["translate"] = TransFrames((0, 0, 0), (0.9, 0, -6), (1.8, 0, 0))
            }
        }
    },
'@
$marker = 'var shulkerAnims = new JsonObject' + $NL + '{'
if (-not $c.Contains('closed_loop')) {
  $c = $c.Replace($marker, $marker + $NL + $closedLoop)
  Write-Host '5. closed_loop anim added'
} else { Write-Host '5. already present' }

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'artgen shulker edits done'

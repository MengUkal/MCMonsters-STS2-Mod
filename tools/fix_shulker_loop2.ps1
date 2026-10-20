$ErrorActionPreference = 'Stop'
$NL = [char]10
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)

# ===== 1. skin: 从full槽移除full_closed，加到shell槽 =====
$oldFull = '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    },' + $NL
if ($c.Contains($oldFull)) {
  $c = $c.Replace($oldFull, '')
  Write-Host '1. full_closed removed from full slot'
} else { Write-Host '1. anchor miss' }
# 找到skins的结尾（full槽结束后）加shell槽
$oldTail = '                    ["full_flash"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 300, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
$newTail = '                    ["full_flash"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 300, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                },' + $NL + '                ["shell"] = new JsonObject' + $NL + '                {' + $NL + '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
if ($c.Contains($oldTail)) { $c = $c.Replace($oldTail, $newTail); Write-Host '2. shell slot skin added' } else { Write-Host '2. tail anchor miss' }

# ===== 2. closed_loop 动画（插在idle_loop之后）=====
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
$anchor = '    ["cast"] = new JsonObject' + $NL + '    {' + $NL + '        // 闭壳动画只做壳缩紧脉冲，状态由closed_loop接管'
if (-not $c.Contains('["closed_loop"]')) {
  $c = $c.Replace($anchor, $closedLoop + $NL + $anchor)
  Write-Host '3. closed_loop anim added'
} else { Write-Host '3. closed_loop already present' }

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'done'

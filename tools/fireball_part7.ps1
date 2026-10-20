$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$NL = [char]10

# 火焰弹插槽块（要保留在cast/attack，其余动画删掉）
$fbSlotBlock = '            ["fireball"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.05, ["name"] = "fireball" },' + $NL + '                    new JsonObject { ["time"] = 0.55 }),' + $NL + '                ["rgba"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.05, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.42, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["color"] = "ffffff00" })' + $NL + '            }'
$fbSlotWithComma = ',' + $NL + $fbSlotBlock

# ===== 1. 烈焰人：从 hurt/die/idle 中删除火焰弹插槽块（保留cast/attack）=====
$av = $c.IndexOf('var blazeAnims = new JsonObject')
$sv = $c.IndexOf('var blazeSkel = new JsonObject', $av)
$anims = $c.Substring($av, $sv - $av)

function StripFireballFromAnim([string]$anims, [string]$animKey) {
    $s = $anims.IndexOf('["' + $animKey + '"] = new JsonObject')
    if ($s -lt 0) { return $anims }
    # 找下一个动画起点或结尾
    $nextKeys = @('["idle_loop"]','["cast"]','["attack"]','["hurt"]','["die"]')
    $e = $anims.Length
    foreach ($k in $nextKeys) {
        $idx = $anims.IndexOf($k + ' = new JsonObject', $s + 5)
        if ($idx -gt $s -and $idx -lt $e) { $e = $idx }
    }
    $block = $anims.Substring($s, $e - $s)
    if ($block.Contains($fbSlotBlock)) {
        $blockNew = $block.Replace($fbSlotWithComma, '')
        $animPrint = '  ' + $animKey + ' -> fireball removed'
        Write-Host $animPrint
        return $anims.Substring(0, $s) + $blockNew + $anims.Substring($e)
    }
    return $anims
}

foreach ($k in @('idle_loop','hurt','die')) {
    $anims = StripFireballFromAnim $anims $k
}
$c = $c.Substring(0, $av) + $anims + $c.Substring($sv)

# ===== 2. 烈焰人 cast/attack：加 fireballBone 关键帧 =====
$fbBone = '            ["fireballBone"] = new JsonObject' + $NL + '            {' + $NL + '                ["translate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.05, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.4, ["x"] = -1500, ["y"] = -30 }),' + $NL + '                ["scale"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.4, ["x"] = 1, ["y"] = 1 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["x"] = 2.2, ["y"] = 2.2 })' + $NL + '            }'

# cast：root块（scale+rotate，无translate）锚rotate行收尾
$oldCastRoot = '                ["rotate"] = RotFrames((0, 0), (0.08, 2), (0.16, -2), (0.24, 2), (0.32, -2), (0.4, 0), (0.6, 0))' + $NL + '            }' + $NL + '        }' + $NL + '    },' + $NL + '    ["attack"] = new JsonObject'
$newCastRoot = '                ["rotate"] = RotFrames((0, 0), (0.08, 2), (0.16, -2), (0.24, 2), (0.32, -2), (0.4, 0), (0.6, 0))' + $NL + '            },' + $NL + $fbBone + $NL + '        }' + $NL + '    },' + $NL + '    ["attack"] = new JsonObject'
if ($c.Contains($oldCastRoot)) { $c = $c.Replace($oldCastRoot, $newCastRoot); Write-Host 'bz cast bones OK' } else { Write-Host 'bz cast bones SKIP' }

# attack：root块（rotate+translate）锚translate行收尾
$oldAtkRoot = '                ["translate"] = TransFrames((0, 0, 0), (0.1, 18, 0), (0.22, -45, 0), (0.38, 12, 0), (0.6, 0, 0))' + $NL + '            }' + $NL + '        }' + $NL + '    },' + $NL + '    ["hurt"] = new JsonObject'
$newAtkRoot = '                ["translate"] = TransFrames((0, 0, 0), (0.1, 18, 0), (0.22, -45, 0), (0.38, 12, 0), (0.6, 0, 0))' + $NL + '            },' + $NL + $fbBone + $NL + '        }' + $NL + '    },' + $NL + '    ["hurt"] = new JsonObject'
if ($c.Contains($oldAtkRoot)) { $c = $c.Replace($oldAtkRoot, $newAtkRoot); Write-Host 'bz attack bones OK' } else { Write-Host 'bz attack bones SKIP' }

# ===== 3. 烈焰人 skins：注册 fireball 插槽附件 =====
$oldBzSkins = '                ["full"] = BuildBlazeAttachments(blazeFrameCount)'
$newBzSkins = '                ["fireball"] = new JsonObject' + $NL + '                {' + $NL + '                    ["fireball"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 0,' + $NL + '                        ["width"] = 140, ["height"] = 140' + $NL + '                    }' + $NL + '                },' + $NL + $oldBzSkins
if ($c.Contains('["fireball"] = new JsonObject' + $NL + '                {' + $NL + '                    ["fireball"]')) { Write-Host 'bz skins SKIP' } elseif ($c.Contains($oldBzSkins)) { $c = $c.Replace($oldBzSkins, $newBzSkins); Write-Host 'bz skins OK' } else { Write-Host 'bz skins SKIP(anchor)' }

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'fireball part7 SAVED'

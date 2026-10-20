$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$NL = [char]10

function ScopeReplace([string]$text, [string]$startMarker, [string]$endMarker, [string]$old, [string]$new, [string]$label) {
    $s = $text.IndexOf($startMarker)
    if ($s -lt 0) { Write-Host "SKIP(start): $label"; return $text }
    $e = $text.IndexOf($endMarker, $s)
    if ($e -lt 0) { Write-Host "SKIP(end): $label"; return $text }
    $scope = $text.Substring($s, $e - $s)
    if (-not $scope.Contains($old)) { Write-Host "SKIP(old): $label"; return $text }
    $scopeNew = $scope.Replace($old, $new)
    Write-Host "OK: $label"
    return $text.Substring(0, $s) + $scopeNew + $text.Substring($e)
}

# ===== 烈焰人：图集加火焰弹列 =====
$oldBzSave = 'frameBitmaps.ForEach(b => b.Dispose());'
$newBzSave = 'AddArrowToAtlasPng(blazeAtlasPng, Path.Combine(blazeDir, "fireball_source.png"), 300 * blazeCols, 0);' + $NL + $oldBzSave
if ($c.Contains('AddArrowToAtlasPng(blazeAtlasPng')) { Write-Host 'bz atlas SKIP' } else { $c = $c.Replace($oldBzSave, $newBzSave); Write-Host 'bz atlas OK' }
# 图集文本：尺寸+火焰弹区域
$c = $c.Replace('$"size:{300 * blazeCols},500"', '$"size:{300 * blazeCols + 140},500"')
$oldBzTxt = 'atlasSb.AppendLine("full_flash").AppendLine($"bounds:{300 * blazeFrameCount},0,300,500");'
if ($c.Contains('AppendLine("fireball")')) { Write-Host 'bz atlas text SKIP' } else {
  $c = $c.Replace($oldBzTxt, $oldBzTxt + $NL + 'atlasSb.AppendLine("fireball").AppendLine($"bounds:{300 * blazeCols},0,140,140");')
  Write-Host 'bz atlas text OK'
}

# ===== 恶魂：图集740宽 + 火焰弹区域 =====
$oldGhComp = 'ComposeTwoFrameAtlas(ghastBaseFrame, ghastAtlasPng);'
$newGhComp = 'ComposeTwoFrameAtlasWithArrow(ghastBaseFrame, Path.Combine(ghastDir, "fireball_source.png"), ghastAtlasPng);'
if ($c.Contains('ComposeTwoFrameAtlasWithArrow(ghastBaseFrame')) { Write-Host 'gh compose SKIP' } else { $c = $c.Replace($oldGhComp, $newGhComp); Write-Host 'gh compose OK' }
$c = $c.Replace("ghast.png`n" + "size:600,500", "ghast.png`n" + "size:740,500")
# ghast atlas块加fireball区域（scoped）
$atlasStart = $c.IndexOf('var ghastAtlas =')
if ($atlasStart -lt 0) { $atlasStart = $c.IndexOf('ghast.png') }
$atlasEnd = $c.IndexOf('File.WriteAllText', $atlasStart)
if ($atlasStart -ge 0 -and $atlasEnd -gt $atlasStart) {
  $scope = $c.Substring($atlasStart, $atlasEnd - $atlasStart)
  if (-not $scope.Contains('bounds:600,100,140,140')) {
    $scopeNew = $scope.Replace("full_flash`n" + 'bounds:300,0,300,500', "full_flash`n" + 'bounds:300,0,300,500' + "`n" + "fireball`n" + 'bounds:600,100,140,140')
    if ($scopeNew -ne $scope) {
      $c = $c.Substring(0, $atlasStart) + $scopeNew + $c.Substring($atlasEnd)
      Write-Host 'gh atlas region OK'
    } else { Write-Host 'gh atlas region SKIP (anchor)' }
  } else { Write-Host 'gh atlas region SKIP (present)' }
} else { Write-Host 'gh atlas region SKIP (span)' }

# ===== JSON：两个怪加火焰弹骨骼/插槽/皮肤 =====
$oldBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" }' + $NL + '    },'
$newBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" },' + $NL + '        new JsonObject { ["name"] = "fireballBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }' + $NL + '    },'
$oldSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }' + $NL + '    },'
$newSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },' + $NL + '        new JsonObject { ["name"] = "fireball", ["bone"] = "fireballBone" }' + $NL + '    },'
$oldSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["full"] = new JsonObject'
$newSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["fireball"] = new JsonObject' + $NL + '                {' + $NL + '                    ["fireball"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 0,' + $NL + '                        ["width"] = 140, ["height"] = 140' + $NL + '                    }' + $NL + '                },' + $NL + '                ["full"] = new JsonObject'

$c = ScopeReplace $c 'var blazeSkel = new JsonObject' '"animations"] = blazeAnims' $oldBones $newBones 'bz bones'
$c = ScopeReplace $c 'var blazeSkel = new JsonObject' '"animations"] = blazeAnims' $oldSlots $newSlots 'bz slots'
$c = ScopeReplace $c 'var blazeSkel = new JsonObject' '"animations"] = blazeAnims' $oldSkins $newSkins 'bz skins'
$c = ScopeReplace $c 'var ghastSkel = new JsonObject' '"animations"] = ghastAnims' $oldBones $newBones 'gh bones'
$c = ScopeReplace $c 'var ghastSkel = new JsonObject' '"animations"] = ghastAnims' $oldSlots $newSlots 'gh slots'
$c = ScopeReplace $c 'var ghastSkel = new JsonObject' '"animations"] = ghastAnims' $oldSkins $newSkins 'gh skins'

# ===== 火焰弹飞行关键帧（直线无抛物线 + 末端爆开淡出）=====
$fbBone = '            ["fireballBone"] = new JsonObject' + $NL + '            {' + $NL + '                ["translate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.05, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.4, ["x"] = -1500, ["y"] = -30 }),' + $NL + '                ["scale"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.4, ["x"] = 1, ["y"] = 1 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["x"] = 2.2, ["y"] = 2.2 })' + $NL + '            }'
$fbSlot = '            ["fireball"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.05, ["name"] = "fireball" },' + $NL + '                    new JsonObject { ["time"] = 0.55 }),' + $NL + '                ["rgba"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.05, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.42, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["color"] = "ffffff00" })' + $NL + '            }'

# 烈焰人cast：有slots（full时间轴）+bones（root scale/rotate/translate）
$oldBzCastSlots = '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["full"] = new JsonObject { ["attachment"] = new JsonArray(new JsonObject { ["time"] = 0, ["name"] = "full" }) }' + $NL + '        },'
$newBzCastSlots = '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["full"] = new JsonObject { ["attachment"] = new JsonArray(new JsonObject { ["time"] = 0, ["name"] = "full" }) },' + $NL + $fbSlot + $NL + '        },'
$c = ScopeReplace $c 'var blazeAnims = new JsonObject' 'var blazeSkel = new JsonObject' $oldBzCastSlots $newBzCastSlots 'bz cast slots'
# 烈焰人cast bones：root块收尾加fireballBone（cast的root块有scale/rotate/translate三行，锚translate行）
$oldBzCastBones = '                ["translate"] = TransFrames((0, 0, 0), (0.45, 0, 0), (0.75, 0, 0), (1.0, 0, 0))' + $NL + '            }' + $NL + '        }' + $NL + '    },'
$oldBzCastBones2 = '                ["translate"] = TransFrames((0, 0, 0), (0.25, 16, 4), (0.45, 16, 4), (0.7, 0, 0))' + $NL + '            }' + $NL + '        }'
$c = ScopeReplace $c 'var blazeAnims = new JsonObject' 'var blazeSkel = new JsonObject' $oldBzCastBones2 ($oldBzCastBones2.Replace('            }' + $NL + '        }', '            },' + $NL + $fbBone + $NL + '        }')) 'bz cast bones'

# 恶魂cast：只有bones（root rotate/translate）→ 加fireballBone + slots
$oldGhCast = '    ["cast"] = new JsonObject' + $NL + '    {' + $NL + '        // 蓄力吐火球：后仰蓄力' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.25, 4), (0.45, 4), (0.7, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.25, 16, 4), (0.45, 16, 4), (0.7, 0, 0))' + $NL + '            }' + $NL + '        }' + $NL + '    },'
$newGhCast = '    ["cast"] = new JsonObject' + $NL + '    {' + $NL + '        // 蓄力吐火球：后仰蓄力 + 火焰弹飞出' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.25, 4), (0.45, 4), (0.7, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.25, 16, 4), (0.45, 16, 4), (0.7, 0, 0))' + $NL + '            },' + $NL + $fbBone + $NL + '        },' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + $fbSlot + $NL + '        }' + $NL + '    },'
$c = ScopeReplace $c 'var ghastAnims = new JsonObject' 'var ghastSkel = new JsonObject' $oldGhCast $newGhCast 'gh cast'

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'fireball part6 SAVED'

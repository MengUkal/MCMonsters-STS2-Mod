$ErrorActionPreference = 'Stop'
$NL = [char]10
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)

# ===== 1. 女巫图集：合成带药水 + 740宽 + potion区域 =====
$oldComp = 'ComposeTwoFrameAtlas(witchBaseFrame, witchAtlasPng);'
$newComp = 'ComposeTwoFrameAtlasWithArrow(witchBaseFrame, Path.Combine(witchDir, "potion_source.png"), witchAtlasPng);'
if ($c.Contains($oldComp)) { $c = $c.Replace($oldComp, $newComp); Write-Host 'witch compose OK' } else { Write-Host 'witch compose SKIP' }
$c = $c.Replace("witch.png`n" + "size:600,500", "witch.png`n" + "size:740,500")
$as = $c.IndexOf('var witchAtlas =')
if ($as -lt 0) { throw 'witch atlas var not found' }
$ae = $c.IndexOf('File.WriteAllText', $as)
$scopeA = $c.Substring($as, $ae - $as)
if (-not $scopeA.Contains('bounds:600,100,140,140')) {
  $scopeA2 = $scopeA.Replace("full_flash`n" + 'bounds:300,0,300,500', "full_flash`n" + 'bounds:300,0,300,500' + "`n" + "potion`n" + 'bounds:600,100,140,140')
  $c = $c.Substring(0, $as) + $scopeA2 + $c.Substring($ae)
  Write-Host 'witch atlas region OK'
} else { Write-Host 'witch atlas region SKIP' }

# ===== 2. 女巫JSON：potion骨骼/插槽/皮肤 =====
$ws = $c.IndexOf('var witchSkel = new JsonObject')
$we = $c.IndexOf('"animations"] = witchAnims', $ws)
if ($ws -lt 0 -or $we -lt 0) { throw 'witch skel span not found' }
$scopeJ = $c.Substring($ws, $we - $ws)

$oldBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" }' + $NL + '    },'
$newBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" },' + $NL + '        new JsonObject { ["name"] = "potionBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }' + $NL + '    },'
if ($scopeJ.Contains($oldBones)) { $scopeJ = $scopeJ.Replace($oldBones, $newBones); Write-Host 'witch bones OK' } else { Write-Host 'witch bones SKIP' }
$oldSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }' + $NL + '    },'
$newSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },' + $NL + '        new JsonObject { ["name"] = "potion", ["bone"] = "potionBone" }' + $NL + '    },'
if ($scopeJ.Contains($oldSlots)) { $scopeJ = $scopeJ.Replace($oldSlots, $newSlots); Write-Host 'witch slots OK' } else { Write-Host 'witch slots SKIP' }
$oldSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["full"] = new JsonObject'
$newSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["potion"] = new JsonObject' + $NL + '                {' + $NL + '                    ["potion"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 0,' + $NL + '                        ["width"] = 140, ["height"] = 140' + $NL + '                    }' + $NL + '                },' + $NL + '                ["full"] = new JsonObject'
if ($scopeJ.Contains($oldSkins)) { $scopeJ = $scopeJ.Replace($oldSkins, $newSkins); Write-Host 'witch skins OK' } else { Write-Host 'witch skins SKIP' }
$c = $c.Substring(0, $ws) + $scopeJ + $c.Substring($we)

# ===== 3. 女巫attack动画：抛物线药水 + 旋转 + 命中爆开淡出 =====
$av = $c.IndexOf('var witchAnims = new JsonObject')
$sv = $c.IndexOf('var witchSkel = new JsonObject', $av)
$scopeAn = $c.Substring($av, $sv - $av)
$oldAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -7), (0.4, 2), (0.6, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.1, 12, 0), (0.22, -30, 0), (0.4, 10, 0), (0.6, 0, 0))' + $NL + '            }' + $NL + '        }' + $NL + '    },'
$newAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        // 投掷药水：抛物线飞出 + 旋转 + 命中爆开' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -7), (0.4, 2), (0.6, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.1, 12, 0), (0.22, -30, 0), (0.4, 10, 0), (0.6, 0, 0))' + $NL + '            },' + $NL + '            ["potionBone"] = new JsonObject' + $NL + '            {' + $NL + '                ["translate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.3, ["x"] = -480, ["y"] = 300 },' + $NL + '                    new JsonObject { ["time"] = 0.45, ["x"] = -950, ["y"] = 150 },' + $NL + '                    new JsonObject { ["time"] = 0.58, ["x"] = -1400, ["y"] = -200 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["angle"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.58, ["angle"] = 250 }),' + $NL + '                ["scale"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.58, ["x"] = 1, ["y"] = 1 },' + $NL + '                    new JsonObject { ["time"] = 0.66, ["x"] = 1.6, ["y"] = 1.6 })' + $NL + '            }' + $NL + '        },' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["potion"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["name"] = "potion" },' + $NL + '                    new JsonObject { ["time"] = 0.66 }),' + $NL + '                ["rgba"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.58, ["color"] = "ffffffff" },' + $NL + '                    new JsonObject { ["time"] = 0.66, ["color"] = "ffffff00" })' + $NL + '            }' + $NL + '        }' + $NL + '    },'
if ($scopeAn.Contains($oldAtk)) { $scopeAn = $scopeAn.Replace($oldAtk, $newAtk); Write-Host 'witch attack anim OK' } else { Write-Host 'witch attack anim SKIP' }
$c = $c.Substring(0, $av) + $scopeAn + $c.Substring($sv)

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'part9 SAVED'

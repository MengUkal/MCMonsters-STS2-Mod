$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)

# 对指定怪物的skel JSON定义域内做替换
function ScopeReplace([string]$text, [string]$startMarker, [string]$endMarker, [string]$old, [string]$new, [string]$label) {
    $s = $text.IndexOf($startMarker)
    if ($s -lt 0) { throw "start not found: $startMarker" }
    $e = $text.IndexOf($endMarker, $s)
    if ($e -lt 0) { throw "end not found: $endMarker" }
    $scope = $text.Substring($s, $e - $s)
    if (-not $scope.Contains($old)) { throw "old not found in scope: $label" }
    $scopeNew = $scope.Replace($old, $new)
    return $text.Substring(0, $s) + $scopeNew + $text.Substring($e)
}

$NL = [char]10
$IND = '    '

# ---- bones 通用（在scope内唯一）----
$oldBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" }' + $NL + '    },'
$newBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" },' + $NL + '        new JsonObject { ["name"] = "arrowBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }' + $NL + '    },'

# ---- slots 通用 ----
$oldSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }' + $NL + '    },'
$newSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },' + $NL + '        new JsonObject { ["name"] = "arrow", ["bone"] = "arrowBone" }' + $NL + '    },'

# ---- skins 通用 ----
$oldSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["full"] = new JsonObject'
$newSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["arrow"] = new JsonObject' + $NL + '                {' + $NL + '                    ["arrow"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 0,' + $NL + '                        ["width"] = 300, ["height"] = 300' + $NL + '                    }' + $NL + '                },' + $NL + '                ["full"] = new JsonObject'

# ===== 骷髅 =====
$c = ScopeReplace $c 'var skelSkel = new JsonObject' '"animations"] = skelAnims' $oldBones $newBones 'skel bones'
Write-Host 'skel bones ok'
$c = ScopeReplace $c 'var skelSkel = new JsonObject' '"animations"] = skelAnims' $oldSlots $newSlots 'skel slots'
Write-Host 'skel slots ok'
$c = ScopeReplace $c 'var skelSkel = new JsonObject' '"animations"] = skelAnims' $oldSkins $newSkins 'skel skins'
Write-Host 'skel skins ok'

# 骷髅攻击动画：arrow插槽时间轴（在full时间轴的闭合后加逗号+arrow）
$oldSkelAtkSlots = '                    new JsonObject { ["time"] = 0.32, ["name"] = "full" })' + $NL + '            }' + $NL + '        },'
$newSkelAtkSlots = '                    new JsonObject { ["time"] = 0.32, ["name"] = "full" })' + $NL + '            },' + $NL + '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.22, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.55 })' + $NL + '            }' + $NL + '        },'
$c = ScopeReplace $c 'var skelAnims = new JsonObject' 'var skelSkel = new JsonObject' $oldSkelAtkSlots $newSkelAtkSlots 'skel attack slots'
Write-Host 'skel attack slots ok'

# 骷髅攻击动画：arrowBone飞行关键帧（root闭合后加逗号+arrowBone）
$oldSkelAtkBones = '                ["translate"] = TransFrames((0, 0, 0), (0.1, 22, 0), (0.22, -55, 0), (0.38, 18, 0), (0.7, 0, 0))' + $NL + '            }' + $NL + '        }'
$newSkelAtkBones = '                ["translate"] = TransFrames((0, 0, 0), (0.1, 22, 0), (0.22, -55, 0), (0.38, 18, 0), (0.7, 0, 0))' + $NL + '            },' + $NL + '            ["arrowBone"] = new JsonObject' + $NL + '            {' + $NL + '                ["translate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["x"] = -1700, ["y"] = -60 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["angle"] = 140 })' + $NL + '            }' + $NL + '        }'
$c = ScopeReplace $c 'var skelAnims = new JsonObject' 'var skelSkel = new JsonObject' $oldSkelAtkBones $newSkelAtkBones 'skel attack bones'
Write-Host 'skel attack bones ok'

# ===== 掠夺者 =====
$c = ScopeReplace $c 'var pillagerSkel = new JsonObject' '"animations"] = pillagerAnims' $oldBones $newBones 'pill bones'
Write-Host 'pill bones ok'
$c = ScopeReplace $c 'var pillagerSkel = new JsonObject' '"animations"] = pillagerAnims' $oldSlots $newSlots 'pill slots'
Write-Host 'pill slots ok'
$c = ScopeReplace $c 'var pillagerSkel = new JsonObject' '"animations"] = pillagerAnims' $oldSkins $newSkins 'pill skins'
Write-Host 'pill skins ok'

# 掠夺者攻击动画：新增slots块（原本没有）+ arrowBone
$oldPillAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -6), (0.4, 2), (0.6, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.1, 10, 0), (0.22, -28, 0), (0.4, 10, 0), (0.6, 0, 0))' + $NL + '            }' + $NL + '        }' + $NL + '    },'
$newPillAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -6), (0.4, 2), (0.6, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.1, 10, 0), (0.22, -28, 0), (0.4, 10, 0), (0.6, 0, 0))' + $NL + '            },' + $NL + '            ["arrowBone"] = new JsonObject' + $NL + '            {' + $NL + '                ["translate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["x"] = -1700, ["y"] = -60 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["angle"] = 140 })' + $NL + '            }' + $NL + '        },' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.5 })' + $NL + '            }' + $NL + '        }' + $NL + '    },'
$c = ScopeReplace $c 'var pillagerSkel = new JsonObject' '"animations"] = pillagerAnims' $oldPillAtk $newPillAtk 'pill attack'
Write-Host 'pill attack ok'

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'part 2 done'

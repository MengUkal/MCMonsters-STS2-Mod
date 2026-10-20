$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$NL = [char]10

function ScopeReplace([string]$text, [string]$startMarker, [string]$endMarker, [string]$old, [string]$new, [string]$label) {
    $s = $text.IndexOf($startMarker)
    if ($s -lt 0) { throw "start not found: $startMarker" }
    $e = $text.IndexOf($endMarker, $s)
    if ($e -lt 0) { throw "end not found: $endMarker" }
    $scope = $text.Substring($s, $e - $s)
    if (-not $scope.Contains($old)) { Write-Host "SKIP (not present): $label"; return $text }
    $scopeNew = $scope.Replace($old, $new)
    Write-Host "OK: $label"
    return $text.Substring(0, $s) + $scopeNew + $text.Substring($e)
}

$oldBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" }' + $NL + '    },'
$newBones = '    ["bones"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "root" },' + $NL + '        new JsonObject { ["name"] = "arrowBone", ["parent"] = "root", ["x"] = 0, ["y"] = 300 }' + $NL + '    },'

$oldSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }' + $NL + '    },'
$newSlots = '    ["slots"] = new JsonArray' + $NL + '    {' + $NL + '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" },' + $NL + '        new JsonObject { ["name"] = "arrow", ["bone"] = "arrowBone" }' + $NL + '    },'

$oldSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["full"] = new JsonObject'
$newSkins = '            ["attachments"] = new JsonObject' + $NL + '            {' + $NL + '                ["arrow"] = new JsonObject' + $NL + '                {' + $NL + '                    ["arrow"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 0,' + $NL + '                        ["width"] = 300, ["height"] = 300' + $NL + '                    }' + $NL + '                },' + $NL + '                ["full"] = new JsonObject'

# ===== 骷髅 =====
$c = ScopeReplace $c 'var skelSkel = new JsonObject' '"animations"] = skelAnims' $oldBones $newBones 'skel bones'
$c = ScopeReplace $c 'var skelSkel = new JsonObject' '"animations"] = skelAnims' $oldSlots $newSlots 'skel slots'
$c = ScopeReplace $c 'var skelSkel = new JsonObject' '"animations"] = skelAnims' $oldSkins $newSkins 'skel skins'

$oldSkelAtkSlots = '                    new JsonObject { ["time"] = 0.32, ["name"] = "full" })' + $NL + '            }' + $NL + '        },'
$newSkelAtkSlots = '                    new JsonObject { ["time"] = 0.32, ["name"] = "full" })' + $NL + '            },' + $NL + '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.22, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.55 })' + $NL + '            }' + $NL + '        },'
$c = ScopeReplace $c 'var skelAnims = new JsonObject' 'var skelSkel = new JsonObject' $oldSkelAtkSlots $newSkelAtkSlots 'skel attack slots'

$oldSkelAtkBones = '                ["translate"] = TransFrames((0, 0, 0), (0.1, 22, 0), (0.22, -55, 0), (0.38, 18, 0), (0.7, 0, 0))' + $NL + '            }' + $NL + '        }'
$newSkelAtkBones = '                ["translate"] = TransFrames((0, 0, 0), (0.1, 22, 0), (0.22, -55, 0), (0.38, 18, 0), (0.7, 0, 0))' + $NL + '            },' + $NL + '            ["arrowBone"] = new JsonObject' + $NL + '            {' + $NL + '                ["translate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["x"] = -1700, ["y"] = -60 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.2, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.55, ["angle"] = 140 })' + $NL + '            }' + $NL + '        }'
$c = ScopeReplace $c 'var skelAnims = new JsonObject' 'var skelSkel = new JsonObject' $oldSkelAtkBones $newSkelAtkBones 'skel attack bones'

# ===== 掠夺者 =====
$c = ScopeReplace $c 'var pillagerSkel = new JsonObject' '"animations"] = pillagerAnims' $oldBones $newBones 'pill bones'
$c = ScopeReplace $c 'var pillagerSkel = new JsonObject' '"animations"] = pillagerAnims' $oldSlots $newSlots 'pill slots'
$c = ScopeReplace $c 'var pillagerSkel = new JsonObject' '"animations"] = pillagerAnims' $oldSkins $newSkins 'pill skins'

[System.IO.File]::WriteAllText($f, $c)
Write-Host 'part 2b SAVED'

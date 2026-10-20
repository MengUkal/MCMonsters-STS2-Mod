$ErrorActionPreference = 'Stop'
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$NL = [char]10

$oldPillAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        // 射击：后坐力' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -6), (0.4, 2), (0.6, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.1, 10, 0), (0.22, -28, 0), (0.4, 10, 0), (0.6, 0, 0))' + $NL + '            }' + $NL + '        }' + $NL + '    },'

$newPillAtk = '    ["attack"] = new JsonObject' + $NL + '    {' + $NL + '        // 射击：后坐力 + 箭矢飞出' + $NL + '        ["bones"] = new JsonObject' + $NL + '        {' + $NL + '            ["root"] = new JsonObject' + $NL + '            {' + $NL + '                ["rotate"] = RotFrames((0, 0), (0.1, 3), (0.22, -6), (0.4, 2), (0.6, 0)),' + $NL + '                ["translate"] = TransFrames((0, 0, 0), (0.1, 10, 0), (0.22, -28, 0), (0.4, 10, 0), (0.6, 0, 0))' + $NL + '            },' + $NL + '            ["arrowBone"] = new JsonObject' + $NL + '            {' + $NL + '                ["translate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["x"] = 0, ["y"] = 0 },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["x"] = -1700, ["y"] = -60 }),' + $NL + '                ["rotate"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["angle"] = 140 },' + $NL + '                    new JsonObject { ["time"] = 0.5, ["angle"] = 140 })' + $NL + '            }' + $NL + '        },' + $NL + '        ["slots"] = new JsonObject' + $NL + '        {' + $NL + '            ["arrow"] = new JsonObject' + $NL + '            {' + $NL + '                ["attachment"] = new JsonArray(' + $NL + '                    new JsonObject { ["time"] = 0.15, ["name"] = "arrow" },' + $NL + '                    new JsonObject { ["time"] = 0.5 })' + $NL + '            }' + $NL + '        }' + $NL + '    },'

if ($c.Contains($oldPillAtk)) {
  $c = $c.Replace($oldPillAtk, $newPillAtk)
  [System.IO.File]::WriteAllText($f, $c)
  Write-Host 'pill attack ok'
} else {
  Write-Host 'PATTERN STILL NOT FOUND'
  exit 1
}

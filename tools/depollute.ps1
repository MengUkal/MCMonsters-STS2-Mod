$ErrorActionPreference = 'Stop'
$NL = [char]10
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)

# ===== 1. 全局移除泄露的 shell 槽位行 =====
$slotLeak = ',' + $NL + '        new JsonObject { ["name"] = "shell", ["bone"] = "root", ["color"] = "ffffffff" }'
$before = ([regex]::Matches($c, [regex]::Escape($slotLeak))).Count
$c = $c.Replace($slotLeak, '')
Write-Host ("移除shell槽位: " + $before)

# ===== 2. 全局移除泄露的 shell 皮肤块 =====
$skinLeak = ',' + $NL + '                ["shell"] = new JsonObject' + $NL + '                {' + $NL + '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
$before2 = ([regex]::Matches($c, [regex]::Escape($skinLeak))).Count
$c = $c.Replace($skinLeak, '')
Write-Host ("移除shell皮肤: " + $before2)

# ===== 3. 只在潜影贝段落重新插入（用mcshulker001定位）=====
$shIndex = $c.IndexOf('"mcshulker001"')
if ($shIndex -lt 0) { throw 'shulker anchor not found' }
$sectionStart = $c.LastIndexOf('var shulkerSkel', $shIndex)
$sectionEnd = $c.IndexOf('shulker.json', $shIndex)
$section = $c.Substring($sectionStart, $sectionEnd - $sectionStart)

# 3a. 潜影贝slots加shell槽
$fullSlotLine = '        new JsonObject { ["name"] = "full", ["bone"] = "root", ["attachment"] = "full", ["color"] = "ffffffff" }'
$shellSlotLine = $fullSlotLine + ',' + $NL + '        new JsonObject { ["name"] = "shell", ["bone"] = "root", ["color"] = "ffffffff" }'
if (-not $section.Contains($shellSlotLine)) {
  $section = $section.Replace($fullSlotLine, $shellSlotLine)
  Write-Host 'shulker shell slot re-added'
}
# 3b. 潜影贝皮肤加shell（找full_flash块的结尾）
$flashTail = '                    ["full_flash"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
$flashWithShell = $flashTail + ',' + $NL + '                ["shell"] = new JsonObject' + $NL + '                {' + $NL + '                    ["full_closed"] = new JsonObject' + $NL + '                    {' + $NL + '                        ["type"] = "region", ["x"] = 0, ["y"] = 250,' + $NL + '                        ["width"] = 300, ["height"] = 500' + $NL + '                    }' + $NL + '                }'
if ($section.Contains($flashTail) -and -not $section.Contains('["shell"] = new JsonObject')) {
  $section = $section.Replace($flashTail, $flashWithShell)
  Write-Host 'shulker shell skin re-added'
}

$c = $c.Substring(0, $sectionStart) + $section + $c.Substring($sectionEnd)
[System.IO.File]::WriteAllText($f, $c)
Write-Host 'de-pollution done'

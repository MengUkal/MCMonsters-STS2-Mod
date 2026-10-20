$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'

# ===== 1. 移除遭遇池诊断日志 =====
$p = Join-Path $root 'src\MCMonsters\Patches\ActEncounterPatches.cs'
$c = [System.IO.File]::ReadAllText($p)
$NL = [char]10
$logBlock = $NL + '        // 诊断：打印注入后的遭遇池内容（验证注入是否生效）' + $NL + '        try' + $NL + '        {' + $NL + '            var __list = System.Linq.Enumerable.ToList(__result);' + $NL + '            System.Console.WriteLine("[MCMonsters] Act" + __instance.Index + " 注入后遭遇池(" + __list.Count + "): " + string.Join(",", System.Linq.Enumerable.Select(__list, e => e.Id.Entry)));' + $NL + '        }' + $NL + '        catch (System.Exception __ex)' + $NL + '        {' + $NL + '            System.Console.WriteLine("[MCMonsters] 日志异常: " + __ex.Message);' + $NL + '        }'
if ($c.Contains('注入后遭遇池')) {
  $c = $c.Replace($logBlock, '')
  [System.IO.File]::WriteAllText($p, $c)
  Write-Host '1. 诊断日志已移除'
} else {
  Write-Host '1. 未找到诊断日志块（可能格式不同）'
  # 兜底：逐行删除包含 注入后遭遇池 的行块
  $lines = $c -split $NL
  $lines | Where-Object { $_ -match '注入后遭遇池' } | ForEach-Object { Write-Host ('   残留行: ' + $_.Trim()) }
}

# ===== 2. 删除未使用的力量类 =====
foreach ($dead in @('GhastChargePower','McSoarPower')) {
  $f = Join-Path $root ("src\MCMonsters\Powers\" + $dead + ".cs")
  if (Test-Path $f) { Remove-Item $f -Force; Write-Host ("2. 删除死代码: " + $dead) }
}

# ===== 3. 清单版本号 1.4.0 -> 1.5.0 =====
$mf = Join-Path $root 'deploy\MCMonsters\MCMonsters.json'
$j = Get-Content $mf -Raw -Encoding UTF8 | ConvertFrom-Json
$j.version = '1.5.0'
$j.description = '将我的世界中的经典怪物加入杀戮尖塔2！一层：僵尸/骷髅/蜘蛛/苦力怕+监守者BOSS；二层：烈焰人/僵尸猪人/凋零骷髅/猪灵蛮兵+凋灵BOSS；三层：末影人/潜影贝/掠夺者/女巫+末影龙BOSS；精英：鸡骑士/恶魂/巨人/掠夺者突袭。Minecraft monsters for Slay the Spire 2!'
$j | ConvertTo-Json -Depth 5 | Set-Content $mf -Encoding UTF8
Write-Host '3. 清单更新为 1.5.0'

# ===== 4. 验证清理结果 =====
$c2 = [System.IO.File]::ReadAllText($p)
Write-Host ("4. 诊断日志残留: " + $c2.Contains('注入后遭遇池'))
$deadLeft = @()
foreach ($dead in @('GhastChargePower','McSoarPower')) {
  if (Test-Path (Join-Path $root ("src\MCMonsters\Powers\" + $dead + ".cs"))) { $deadLeft += $dead }
}
Write-Host ("   死代码残留: " + $(if ($deadLeft) { $deadLeft -join ',' } else { '无' }))

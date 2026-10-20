$ErrorActionPreference = 'Stop'
$NL = [char]10
$p = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Monsters\Shulker.cs'
$c = [System.IO.File]::ReadAllText($p)
# 移除自定义动画器（回到基类标准动画器，稳定）
$s = $c.IndexOf('    // 动画状态机：闭壳后进入闭壳待机循环')
$e = $c.IndexOf('    // 闭壳防御', $s)
if ($s -ge 0 -and $e -gt $s) {
  $c = $c.Substring(0, $s) + $c.Substring($e)
  [System.IO.File]::WriteAllText($p, $c)
  Write-Host 'custom animator removed (back to base)'
} else { Write-Host 'anchor miss' }
# 清掉不再需要的using
$c = [System.IO.File]::ReadAllText($p)
$c = $c.Replace('using MegaCrit.Sts2.Core.Animation;' + $NL, '')
$c = $c.Replace('using MegaCrit.Sts2.Core.Bindings.MegaSpine;' + $NL, '')
[System.IO.File]::WriteAllText($p, $c)
Write-Host 'unused usings cleaned'

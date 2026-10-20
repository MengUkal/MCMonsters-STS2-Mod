$ErrorActionPreference = 'Stop'
$NL = [char]10
$p = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Monsters\Shulker.cs'
$c = [System.IO.File]::ReadAllText($p)

# 1. 移除自定义动画器（整段）
$s = $c.IndexOf('    // 动画状态机（参考官方噬尸蛞蝓')
$e = $c.IndexOf('    // 闭壳防御：格挡26', $s)
if ($s -ge 0 -and $e -gt $s) {
  $c = $c.Substring(0, $s) + $c.Substring($e)
  Write-Host 'custom animator removed'
} else { Write-Host 'animator anchor miss' }

# 2. 移除 IsShellClosed 属性
$propBlock = '    /// <summary>壳甲状态：true=闭壳（立绘显示紫色方块）。供动画器条件谓词读取（参考官方噬尸蛞蝓的IsRavenous模式）。</summary>' + $NL + '    public bool IsShellClosed { get; private set; }' + $NL + $NL
$c = $c.Replace($propBlock, '')
Write-Host ("IsShellClosed removed: " + (-not $c.Contains('IsShellClosed')))

# 3. 移除招式里的状态赋值
$c = $c.Replace('        IsShellClosed = true;' + $NL, '')
$c = $c.Replace('        IsShellClosed = false;' + $NL, '')

# 4. 清理不用的using
$c = $c.Replace('using MegaCrit.Sts2.Core.Animation;' + $NL, '')
$c = $c.Replace('using MegaCrit.Sts2.Core.Bindings.MegaSpine;' + $NL, '')

[System.IO.File]::WriteAllText($p, $c)
Write-Host 'shulker reverted to base animator (壳甲机制保留)'

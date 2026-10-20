$ErrorActionPreference = 'Stop'
$NL = [char]10
$p = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Monsters\Shulker.cs'
$c = [System.IO.File]::ReadAllText($p)
$old = '        cast.NextState = closedIdle;   // 闭壳 → 闭壳待机' + $NL + '        closedIdle.NextState = idle;' + $NL + '        attack.NextState = idle;       // 开壳 → 普通待机' + $NL + '        hurt.NextState = idle;' + $NL + '        return new CreatureAnimator(idle, controller);'
$new = '        cast.NextState = closedIdle;   // 闭壳 → 闭壳待机（立绘切换为紫色方块）' + $NL + '        closedIdle.NextState = idle;' + $NL + '        attack.NextState = idle;       // 开壳 → 普通待机（立绘恢复）' + $NL + '        hurt.NextState = idle;' + $NL + '        die.NextState = die;' + $NL + '        CreatureAnimator animator = new CreatureAnimator(idle, controller);' + $NL + '        animator.AddAnyState("Idle", idle);' + $NL + '        animator.AddAnyState("Cast", cast);' + $NL + '        animator.AddAnyState("Attack", attack);' + $NL + '        animator.AddAnyState("Dead", die);' + $NL + '        animator.AddAnyState("Hit", hurt);' + $NL + '        return animator;'
if ($c.Contains($old)) { $c = $c.Replace($old, $new); [System.IO.File]::WriteAllText($p, $c); Write-Host 'animator fixed with AnyStates' } else { Write-Host 'anchor miss' }

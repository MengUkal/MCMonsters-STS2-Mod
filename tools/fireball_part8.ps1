$ErrorActionPreference = 'Stop'
$NL = [char]10

# ===== 1. 恶魂火焰弹俯冲轨迹（右上→左下）=====
$f = 'D:\塔2MOD\MCMonsters\tools\ArtGen\Program.cs'
$c = [System.IO.File]::ReadAllText($f)
$gs = $c.IndexOf('var ghastAnims = new JsonObject')
$ge = $c.IndexOf('var ghastSkel = new JsonObject', $gs)
if ($gs -lt 0 -or $ge -lt 0) { throw 'ghast anims span not found' }
$scope = $c.Substring($gs, $ge - $gs)
$old = 'new JsonObject { ["time"] = 0.4, ["x"] = -1500, ["y"] = -30 })'
$new = 'new JsonObject { ["time"] = 0.4, ["x"] = -1400, ["y"] = -330 })'
$cnt = 0
while ($scope.Contains($old)) { $scope = $scope.Replace($old, $new); $cnt++ }
$c = $c.Substring(0, $gs) + $scope + $c.Substring($ge)
[System.IO.File]::WriteAllText($f, $c)
Write-Host ("ghast fireball descent: " + $cnt + " 处")

# ===== 2. Ghast.cs 与 Blaze.cs 加点燃特效 =====
$igniteMethod = @'

    // 火焰弹命中：给玩家挂官方点燃特效（实验体燃烧VFX，带闪烁淡入淡出）
    private async Task IgnitePlayer(IReadOnlyList<Creature> targets)
    {
        foreach (var t in targets)
        {
            if (t.IsPlayer)
            {
                t.GetBackVfxContainer()?.AddChildSafely(global::MegaCrit.Sts2.Core.Nodes.Vfx.NTestSubjectBurnVfx.Create());
            }
        }
        await Task.CompletedTask;
    }
'@

function AddIgnite([string]$path, [string[]]$anchors, [string]$label) {
    $t = [System.IO.File]::ReadAllText($path)
    if ($t.Contains('IgnitePlayer')) { Write-Host ($label + ' SKIP (present)'); return }
    foreach ($a in $anchors) {
        if (-not $t.Contains($a)) { Write-Host ($label + ' anchor missing: ' + $a.Substring(0, [Math]::Min(40, $a.Length))) }
    }
    # 在最后一个Execute(null);后插入Ignite调用
    $lastExec = $t.LastIndexOf('            .Execute(null);')
    if ($lastExec -lt 0) { throw ($label + ': Execute anchor not found') }
    $insertAt = $lastExec + '            .Execute(null);'.Length
    $call = $NL + '        await IgnitePlayer(targets);'
    $t2 = $t.Substring(0, $insertAt) + $call + $t.Substring($insertAt)
    # 在类的最后（最后一个 } 之前）插入IgnitePlayer方法
    $lastBrace = $t2.LastIndexOf('}')
    $t2 = $t2.Substring(0, $lastBrace) + $igniteMethod + $NL + $t2.Substring($lastBrace)
    [System.IO.File]::WriteAllText($path, $t2)
    Write-Host ($label + ' ignite added')
}

# Ghast：三个攻击各自插入（Execute在Attack1/2/3里，一起插到最后一个=Attack3）
$gf = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Monsters\Ghast.cs'
$g = [System.IO.File]::ReadAllText($gf)
if (-not $g.Contains('IgnitePlayer')) {
    $g = $g.Replace('.WithHitFx("vfx/vfx_attack_blunt")' + $NL + '            .Execute(null);' + $NL + '    }', '.WithHitFx("vfx/vfx_attack_blunt")' + $NL + '            .Execute(null);' + $NL + '        await IgnitePlayer(targets);' + $NL + '    }')
    $lastBrace = $g.LastIndexOf('}')
    $g = $g.Substring(0, $lastBrace) + $igniteMethod + $NL + $g.Substring($lastBrace)
    [System.IO.File]::WriteAllText($gf, $g)
    Write-Host 'Ghast ignite added'
} else { Write-Host 'Ghast SKIP' }

$bf = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Monsters\Blaze.cs'
$b = [System.IO.File]::ReadAllText($bf)
if (-not $b.Contains('IgnitePlayer')) {
    $b = $b.Replace('await PlayerAttackBurn(targets);', 'await PlayerAttackBurn(targets);' + $NL + '        await IgnitePlayer(targets);')
    $lastBrace = $b.LastIndexOf('}')
    $b = $b.Substring(0, $lastBrace) + $igniteMethod + $NL + $b.Substring($lastBrace)
    [System.IO.File]::WriteAllText($bf, $b)
    Write-Host 'Blaze ignite added'
} else { Write-Host 'Blaze SKIP' }

Write-Host 'part8 done'

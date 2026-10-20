$ErrorActionPreference = 'Stop'
$NL = [char]10
$mdir = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Monsters'

function AddAttackSfx([string]$file, [string]$snd) {
    $p = Join-Path $mdir $file
    if (-not (Test-Path $p)) { Write-Host ("SKIP missing " + $file); return }
    $c = [System.IO.File]::ReadAllText($p)
    if ($c.Contains($snd)) { Write-Host ("SKIP present " + $file); return }
    $pattern = '([ \t]*\.WithAttackerAnim\("[^"]+", [\d.]+f\))'
    $replacement = '$1' + $NL + '            .WithAttackerFx(null, null, "' + $snd + '")'
    $c2 = [regex]::Replace($c, $pattern, $replacement)
    if ($c2 -ne $c) { [System.IO.File]::WriteAllText($p, $c2); Write-Host ("OK attack sfx " + $file) }
    else { Write-Host ("NO MATCH " + $file) }
}

function InsertAfter([string]$file, [string]$anchor, [string]$insert, [string]$label) {
    $p = Join-Path $mdir $file
    if (-not (Test-Path $p)) { Write-Host ("SKIP missing " + $file); return }
    $c = [System.IO.File]::ReadAllText($p)
    if ($c.Contains($insert.Trim())) { Write-Host ("SKIP present " + $label); return }
    if (-not $c.Contains($anchor)) { Write-Host ("ANCHOR MISS " + $label + " in " + $file); return }
    $c = $c.Replace($anchor, $anchor + $NL + $insert)
    [System.IO.File]::WriteAllText($p, $c)
    Write-Host ("OK " + $label)
}

# ===== 攻击音效（正则统一挂）=====
AddAttackSfx 'Zombie.cs' 'mc_zombie_attack.ogg'
AddAttackSfx 'Spider.cs' 'mc_spider_attack.ogg'
AddAttackSfx 'Enderman.cs' 'mc_enderman_teleport.ogg'
AddAttackSfx 'ZombiePiglin.cs' 'mc_zombiepiglin_attack.ogg'
AddAttackSfx 'WitherSkeleton.cs' 'mc_skeleton_attack.ogg'
AddAttackSfx 'PiglinBrute.cs' 'mc_brute_attack.ogg'
AddAttackSfx 'WitherBoss.cs' 'mc_wither_attack.ogg'
AddAttackSfx 'Shulker.cs' 'mc_shulker_shoot.ogg'
AddAttackSfx 'Giant.cs' 'mc_giant_attack.ogg'
AddAttackSfx 'EnderDragon.cs' 'mc_dragon_attack.ogg'
AddAttackSfx 'ChickenJockey.cs' 'mc_chicken_attack.ogg'
AddAttackSfx 'Warden.cs' 'mc_warden_attack.ogg'

$play = '        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("'

# ===== 非攻击招式音效（直调）=====
# 女巫喝药
InsertAfter 'Witch.cs' 'await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' ($play + 'mc_witch_drink.ogg");') 'witch drink'
# 僵尸猪人蓄怒（愤怒声）
InsertAfter 'ZombiePiglin.cs' 'await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' ($play + 'mc_zombiepiglin_angry.ogg");') 'piglin calm angry'
# 潜影贝闭壳
InsertAfter 'Shulker.cs' 'await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' ($play + 'mc_shulker_close.ogg");') 'shulker close'
# 苦力怕嘶嘶（3次蓄力，TriggerAnim Cast 0.4f）
InsertAfter 'Creeper.cs' 'await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' ($play + 'mc_creeper_hiss.ogg");') 'creeper hiss'
# 末影人瞬移（TeleportMove 的 TriggerAnim Cast 0.4f）
InsertAfter 'Enderman.cs' 'await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' ($play + 'mc_enderman_teleport.ogg");') 'enderman teleport'
# 监守者音波（直接Damage，无WithAttackerAnim — 在TriggerAnim后加音效+特效）
InsertAfter 'Warden.cs' 'await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' ($play + 'mc_warden_attack.ogg");' + $NL + '        global::MegaCrit.Sts2.Core.Commands.VfxCmd.PlayOnCreatures(targets, "vfx/common/vfx_common_outward_streaks");') 'warden sonic vfx'

Write-Host 'sound wiring done'

$ErrorActionPreference = 'Stop'
$NL = [char]10
$p = 'D:\塔2MOD\MCMonsters\src\MCMonsters\Monsters\Shulker.cs'
$c = [System.IO.File]::ReadAllText($p)

# 1. 加using
if (-not $c.Contains('using MegaCrit.Sts2.Core.Animation;')) {
  $c = $c.Replace('using MegaCrit.Sts2.Core.Commands;', 'using MegaCrit.Sts2.Core.Animation;' + $NL + 'using MegaCrit.Sts2.Core.Bindings.MegaSpine;' + $NL + 'using MegaCrit.Sts2.Core.Commands;')
}
# 2. 加闭壳状态属性
if (-not $c.Contains('IsShellClosed')) {
  $anchor = '    private int ShellBlock =>'
  $prop = '    /// <summary>壳甲状态：true=闭壳（立绘显示紫色方块）。供动画器条件谓词读取（参考官方噬尸蛞蝓的IsRavenous模式）。</summary>' + $NL + '    public bool IsShellClosed { get; private set; }' + $NL + $NL + $anchor
  $c = $c.Replace($anchor, $prop)
}
# 3. 加动画器（噬尸蛞蝓模式：条件谓词选Idle）
$anim = @'
    // 动画状态机（参考官方噬尸蛞蝓 IsRavenous 模式）：
    // 闭壳状态时，引擎每次请求 Idle 都会按谓词选择 closed_loop（闭壳立绘循环）
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new AnimState("idle_loop", isLooping: true);
        AnimState closedIdle = new AnimState("closed_loop", isLooping: true);
        AnimState cast = new AnimState("cast");
        AnimState attack = new AnimState("attack");
        AnimState hurt = new AnimState("hurt");
        AnimState die = new AnimState("die");
        cast.NextState = closedIdle;   // 闭壳动作结束 → 闭壳待机
        closedIdle.NextState = idle;
        attack.NextState = idle;       // 开壳攻击结束 → 开壳待机
        hurt.NextState = idle;
        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        // 关键：Idle 双注册 + 条件谓词（引擎每次 Idle 时按状态选）
        animator.AddAnyState("Idle", idle, () => !IsShellClosed);
        animator.AddAnyState("Idle", closedIdle, () => IsShellClosed);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Hit", hurt);
        return animator;
    }

'@
if (-not $c.Contains('closed_loop')) {
  $c = $c.Replace('    // 闭壳防御：格挡26', $anim + '    // 闭壳防御：格挡26')
  Write-Host 'animator added'
} else { Write-Host 'animator already present' }
# 4. 闭壳招式设置状态true；开壳招式设置false
$c = $c.Replace('        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' + $NL + '        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_shulker_close.ogg");', '        IsShellClosed = true;' + $NL + '        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);' + $NL + '        global::MegaCrit.Sts2.Core.Audio.Debug.NNDebugAudioManager.Instance?.Play("mc_shulker_close.ogg");'.Replace('NNDebugAudioManager','NDebugAudioManager'))
$c = $c.Replace('        await PowerCmd.Remove<ShulkerShellPower>(base.Creature);' + $NL + '        await DamageCmd.Attack(BarrageDamage)', '        IsShellClosed = false;' + $NL + '        await PowerCmd.Remove<ShulkerShellPower>(base.Creature);' + $NL + '        await DamageCmd.Attack(BarrageDamage)')
[System.IO.File]::WriteAllText($p, $c)
Write-Host 'shulker state flags added'

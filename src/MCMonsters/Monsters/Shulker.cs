using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：潜影贝（第3幕普通怪）
/// 被动【壳甲】：闭壳期间受到的所有攻击伤害减半（闭壳招式应用壳甲状态）。
/// 招式循环：闭壳防御（格挡26+壳甲+闭壳贴图）→ 开壳弹幕（2×8+力量+1+移除壳甲+开壳贴图）。
/// 数值：165血（进阶173）。
/// 双潜影贝（ShulkerPair）起手错开：一只以闭合防御开始，一只以开壳弹幕开始。
/// </summary>
public sealed class Shulker : MonsterModel
{
    private bool _startOnBarrage;

    // MC潜影贝30HP且闭壳高防 → STS2第3幕 165（坦克定位）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 173, 165);
    public override int MaxInitialHp => MinInitialHp;

    private int ShellBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 28, 26);
    private int BarrageDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int BarrageHits => 2;
    private int BarrageStrength => 1;

    /// <summary>
    /// 首个招式是否为开壳弹幕（默认闭合防御）。
    /// 由 ShulkerPair 在生成时设置，保证两只起手错开。
    /// </summary>
    public bool StartOnBarrage
    {
        get => _startOnBarrage;
        set
        {
            AssertMutable();
            _startOnBarrage = value;
        }
    }

    // AI循环：闭壳防御 → 开壳弹幕 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState close = new MoveState("CLOSE_SHELL", CloseShellMove, new DefendIntent());
        MoveState barrage = new MoveState("OPEN_BARRAGE", OpenBarrageMove, new BuffIntent(), new MultiAttackIntent(BarrageDamage, BarrageHits));

        close.FollowUpState = barrage;
        barrage.FollowUpState = close;

        states.Add(close);
        states.Add(barrage);

        return new MonsterMoveStateMachine(states, _startOnBarrage ? barrage : close);
    }

    // 闭壳防御：格挡26 + 壳甲（受伤减半，换闭壳贴图）
    private async Task CloseShellMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_shulker_close.ogg");
        await CreatureCmd.GainBlock(base.Creature, ShellBlock, ValueProp.Unpowered, null);
        await PowerCmd.Apply<ShulkerShellPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 开壳弹幕：2×8 + 力量+1（先攻击后蓄力，意图数值不骗人；换回开壳贴图）
    private async Task OpenBarrageMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Remove<ShulkerShellPower>(base.Creature);
        await DamageCmd.Attack(BarrageDamage).WithHitCount(BarrageHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_shulker_shoot.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, BarrageStrength, base.Creature, null);
    }
}

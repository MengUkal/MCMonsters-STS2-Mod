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
/// 我的世界：掠夺者（第3幕普通怪）
/// 被动【装填中断】：装填后若在下一回合累计受到12点以上伤害，弩箭被击落——攻击取消变回装填。
/// 招式循环：装填瞄准（格挡12+装填标记）→ 精准射击（18，可被打断）→ 散射（2×9）。
/// 数值：150血（进阶158）。
/// </summary>
public sealed class Pillager : MonsterModel
{
    private MoveState? _reloadState;

    // MC掠夺者24HP → STS2第3幕 150
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 158, 150);
    public override int MaxInitialHp => MinInitialHp;

    private int ReloadBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 12);
    private int ShotDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);
    private int ScatterDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);
    private int ScatterHits => 2;

    private MoveState ReloadState
    {
        get => _reloadState!;
        set
        {
            AssertMutable();
            _reloadState = value;
        }
    }

    // 由 PillagerLoadPower 在受到12+伤害时调用：攻击取消，变回装填
    public async Task CancelLoadedShot()
    {
        // 移除装填标记（否则会持续累计伤害反复取消攻击）
        await PowerCmd.Remove<PillagerLoadPower>(base.Creature);
        if (ReloadState != null)
        {
            SetMoveImmediate(ReloadState, forceTransition: true);
        }
    }

    // AI循环：装填 → 精准射击 → 散射 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState reload = new MoveState("RELOAD", McGuard.Move("RELOAD", ReloadMove), new DefendIntent());
        MoveState shot = new MoveState("AIMED_SHOT", McGuard.Move("AIMED_SHOT", AimedShotMove), new SingleAttackIntent(ShotDamage));
        MoveState scatter = new MoveState("SCATTER", McGuard.Move("SCATTER", ScatterMove), new MultiAttackIntent(ScatterDamage, ScatterHits));
        ReloadState = reload;

        reload.FollowUpState = shot;
        shot.FollowUpState = scatter;
        scatter.FollowUpState = reload;

        states.Add(reload);
        states.Add(shot);
        states.Add(scatter);

        return new MonsterMoveStateMachine(states, reload);
    }

    // 装填瞄准：格挡12 + 装填标记
    private async Task ReloadMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_pillager_attack.ogg");
        await CreatureCmd.GainBlock(base.Creature, ReloadBlock, ValueProp.Unpowered, null);
        await PowerCmd.Apply<PillagerLoadPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 精准射击：18（装填状态下打出，移除装填标记）
    private async Task AimedShotMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Remove<PillagerLoadPower>(base.Creature);
        await DamageCmd.Attack(ShotDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, null, "mc_pillager_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 散射：2×9
    private async Task ScatterMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ScatterDamage).WithHitCount(ScatterHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_pillager_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}

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
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：监守者（Warden，第1幕BOSS）
/// 原版行为映射：深暗之域的恐怖存在——循声而动，你若是静止不动（空过回合），它便会察觉并积蓄力量。
/// 被动【警觉】：玩家本回合未打出任何牌便结束回合 → 意图立刻变为【蓄力】（力量+2）。
/// 招式循环：重拳18 → 黑暗降临（虚弱1+脆弱1）→ 狂怒连击（2×7）。
/// 数值：第1幕BOSS 220血（进阶240）。
/// </summary>
public sealed class Warden : MonsterModel
{
    private MoveState? _strengthUpState;

    // MC监守者500HP（全游戏最高）→ STS2第1幕BOSS 220
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 240, 220);
    public override int MaxInitialHp => MinInitialHp;

    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);
    private int DarknessWeak => 1;
    private int DarknessFrail => 1;
    private int FuryDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private int FuryHits => 2;
    private int StrengthUpAmount => 2;

    private MoveState StrengthUpState
    {
        get => _strengthUpState!;
        set
        {
            AssertMutable();
            _strengthUpState = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<WardenPunishPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 由 WardenPunishPower 在玩家空过回合时调用：意图立刻变为蓄力
    public async Task PunishIdleTurn()
    {
        if (StrengthUpState != null)
        {
            SetMoveImmediate(StrengthUpState, forceTransition: true);
        }
        await Task.CompletedTask;
    }

    // AI循环：重拳 → 黑暗降临 → 狂怒连击 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState smash = new MoveState("SMASH", SmashMove, new SingleAttackIntent(SmashDamage));
        MoveState darkness = new MoveState("DARKNESS", DarknessMove, new DebuffIntent());
        MoveState fury = new MoveState("FURY", FuryMove, new MultiAttackIntent(FuryDamage, FuryHits));
        MoveState strengthUp = new MoveState("STRENGTH_UP", StrengthUpMove, new BuffIntent());
        StrengthUpState = strengthUp;

        smash.FollowUpState = darkness;
        darkness.FollowUpState = fury;
        fury.FollowUpState = smash;
        strengthUp.FollowUpState = smash;

        states.Add(smash);
        states.Add(darkness);
        states.Add(fury);
        states.Add(strengthUp);

        return new MonsterMoveStateMachine(states, smash);
    }

    // 重拳：毁灭性近战重击
    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SmashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_warden_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    // 黑暗降临：黑暗缠绕（原版缠结debuff——攻击牌费用+1）
    private async Task DarknessMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_warden_attack.ogg");
        await PowerCmd.Apply<TangledPower>(new ThrowingPlayerChoiceContext(), targets, DarknessWeak, Creature, null);
    }

    // 狂怒连击：2×7
    private async Task FuryMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FuryDamage).WithHitCount(FuryHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_warden_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 蓄力（空过惩罚）：力量+2
    private async Task StrengthUpMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, StrengthUpAmount, Creature, null);
    }
}

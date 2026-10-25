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
/// 我的世界：凋零骷髅（第2幕普通怪）
/// 原版行为映射：下界要塞的黑骨弓手，凋零之触——被它打中就会凋零缠身。
/// 被动【凋零之触】：攻击未被格挡时给予玩家1层凋零（每回合失去1点生命/层，可叠加，不衰减）。
/// 招式循环：斩击（8）→ 防御姿态（格挡16+虚弱2）→ 重击（12）。
/// 数值：58血（进阶62）。
/// </summary>
public sealed class WitherSkeleton : MonsterModel
{
    // MC凋零骷髅20HP → STS2第2幕 58（进阶62）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 62, 58);
    public override int MaxInitialHp => MinInitialHp;

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int SlashFrail => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
    private int GuardBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 16);
    private int GuardWeak => 2;
    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<WitherSkeletonCursePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // AI循环：斩击 → 防御姿态 → 重击 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState slash = new MoveState("SLASH", McGuard.Move("SLASH", SlashMove), new SingleAttackIntent(SlashDamage), new DebuffIntent());
        MoveState guard = new MoveState("GUARD", McGuard.Move("GUARD", GuardMove), new DefendIntent(), new DebuffIntent());
        MoveState smash = new MoveState("SMASH", McGuard.Move("SMASH", SmashMove), new SingleAttackIntent(SmashDamage));

        slash.FollowUpState = guard;
        guard.FollowUpState = smash;
        smash.FollowUpState = slash;

        states.Add(slash);
        states.Add(guard);
        states.Add(smash);

        return new MonsterMoveStateMachine(states, slash);
    }

    // 斩击：石剑斩击8+脆弱2（未被格挡则触发凋零之触）
    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, null, "mc_skeleton_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, SlashFrail, Creature, null);
    }

    // 防御姿态：举盾防御（格挡16）并施加虚弱2
    private async Task GuardMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        await CreatureCmd.GainBlock(base.Creature, GuardBlock, ValueProp.Unpowered, null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, GuardWeak, Creature, null);
    }

    // 重击：双手重砸（12）
    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SmashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_skeleton_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}

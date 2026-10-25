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

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：蜘蛛（第1幕普通怪）
/// 原版行为映射：低身快速、扑咬、夜间红眼敌对；游戏化处理——蛛网缠人上易伤，贴墙攀爬躲格挡。
/// 招式循环：扑咬 → 蛛网缠绕 → 扑咬 → 侧壁攀爬。
/// 数值对标第1幕普通怪（血量中等、威胁靠易伤联动）。
/// </summary>
public sealed class Spider : MonsterModel
{
    // 数值（22血/攻击6；高进阶血+4、单发+1）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 22);
    public override int MaxInitialHp => MinInitialHp;

    private int PounceDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private int WebWeak => 1;
    private int WebVulnerable => 1;
    private int ClimbBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 6);

    // AI循环：扑咬 → 蛛网缠绕 → 扑咬 → 侧壁攀爬 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState pounce = new MoveState("POUNCE", McGuard.Move("POUNCE", PounceMove), new SingleAttackIntent(PounceDamage));
        MoveState webSnare = new MoveState("WEB_SNARE", McGuard.Move("WEB_SNARE", WebSnareMove), new DebuffIntent());
        MoveState pounce2 = new MoveState("POUNCE2", McGuard.Move("POUNCE2", PounceMove), new SingleAttackIntent(PounceDamage));
        MoveState sideClimb = new MoveState("SIDE_CLIMB", McGuard.Move("SIDE_CLIMB", SideClimbMove), new DefendIntent());

        pounce.FollowUpState = webSnare;
        webSnare.FollowUpState = pounce2;
        pounce2.FollowUpState = sideClimb;
        sideClimb.FollowUpState = pounce;

        states.Add(pounce);
        states.Add(webSnare);
        states.Add(pounce2);
        states.Add(sideClimb);

        return new MonsterMoveStateMachine(states, pounce);
    }

    // 扑咬：压低身子猛扑一口
    private async Task PounceMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(PounceDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_spider_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 蛛网缠绕：喷丝裹住玩家（虚弱1 + 脆弱1）
    // 注意 Apply 单目标重载是 (ctx, target, amount, applier, ...)
    private async Task WebSnareMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets[0], WebWeak, Creature, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets[0], WebVulnerable, Creature, null);
    }

    // 侧壁攀爬：爬上侧壁躲开攻击（格挡）
    private async Task SideClimbMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        await CreatureCmd.GainBlock(base.Creature, ClimbBlock, ValueProp.Unpowered, null);
    }
}

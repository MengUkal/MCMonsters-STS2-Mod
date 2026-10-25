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
/// 我的世界：烈焰人（第2幕普通怪）
/// 原版行为映射：下界要塞的火焰守卫——悬浮升空躲避攻击，俯冲射出火球弹幕，浑身火焰灼烧靠近者。
/// 被动【烈焰之躯】：每次攻击时玩家失去2点生命（无视格挡）。
/// 招式循环：火球术（攻击10+获得翱翔+格挡12）→ 火焰弹幕（3×4+失去翱翔）→ 蓄能（力量+1+格挡10）。
/// 数值：74血（进阶78）。
/// </summary>
public sealed class Blaze : MonsterModel
{
    // MC烈焰人20HP → STS2第2幕 74（进阶78）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 74);
    public override int MaxInitialHp => MinInitialHp;

    private int FireballDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int SoarBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 13, 12);
    private int BarrageDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int BarrageHits => 4;
    private int AttackBurn => 2;
    private int BuffStrength => 1;
    private int BuffBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 10);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<BlazeAuraPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // AI循环：火球术（升空）→ 火焰弹幕（俯冲）→ 蓄能 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState soar = new MoveState("SOAR_STRIKE", McGuard.Move("SOAR_STRIKE", SoarStrikeMove), new SingleAttackIntent(FireballDamage), new DefendIntent());
        MoveState barrage = new MoveState("BARRAGE", McGuard.Move("BARRAGE", BarrageMove), new MultiAttackIntent(BarrageDamage, BarrageHits));
        MoveState buff = new MoveState("BUFF_UP", McGuard.Move("BUFF_UP", BuffUpMove), new BuffIntent(), new DefendIntent());

        soar.FollowUpState = barrage;
        barrage.FollowUpState = buff;
        buff.FollowUpState = soar;

        states.Add(soar);
        states.Add(barrage);
        states.Add(buff);

        return new MonsterMoveStateMachine(states, soar);
    }

    // 火球术：攻击10+升空获得翱翔（格挡12）
    private async Task SoarStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FireballDamage).FromMonster(this)
            .WithAttackerAnim("Cast", 0.3f)
            .WithAttackerFx(null, null, "mc_blaze_attack.ogg")
            .WithHitFx("vfx/fire_impact/vfx_fire_burst_center_flipbook")
            .Execute(null);
        // 每个附加效果单独兜底：任何一步失败都不该影响后面的效果，更不能卡住敌方回合
        await McGuard.Run("BLAZE_SOAR_STRIKE.burn", () => PlayerAttackBurn(targets));
        await McGuard.Run("BLAZE_SOAR_STRIKE.ignite", () => IgnitePlayer(targets));
        await McGuard.Run("BLAZE_SOAR_STRIKE.block", () => CreatureCmd.GainBlock(base.Creature, SoarBlock, ValueProp.Unpowered, null));
        // 官方SoarPower：翱翔期间受到的攻击伤害减少50%（自带游戏本地化）
        await McGuard.Run("BLAZE_SOAR_STRIKE.soar", () =>
            PowerCmd.Apply<global::MegaCrit.Sts2.Core.Models.Powers.SoarPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null));
    }

    // 火焰弹幕：俯冲四连射（3×4），失去翱翔
    private async Task BarrageMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BarrageDamage).WithHitCount(BarrageHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_blaze_attack.ogg")
            .WithHitFx("vfx/fire_impact/vfx_fire_burst_center_flipbook")
            .Execute(null);
        await McGuard.Run("BLAZE_BARRAGE.burn", () => PlayerAttackBurn(targets));
        await McGuard.Run("BLAZE_BARRAGE.ignite", () => IgnitePlayer(targets));
        // 俯冲落地：失去翱翔
        await McGuard.Run("BLAZE_BARRAGE.soar_off", () => PowerCmd.Remove<global::MegaCrit.Sts2.Core.Models.Powers.SoarPower>(base.Creature));
    }

    // 蓄能：力量+1并获得10点护盾
    private async Task BuffUpMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, BuffStrength, Creature, null);
        await CreatureCmd.GainBlock(base.Creature, BuffBlock, ValueProp.Unpowered, null);
    }

    // 烈焰之躯被动：每次攻击玩家失去2点生命（无视格挡）
    private async Task PlayerAttackBurn(IReadOnlyList<Creature> targets)
    {
        if (targets.Count > 0)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), targets[0], AttackBurn, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        }
    }

    // 火焰弹命中：给玩家挂官方点燃特效（实验体燃烧VFX，带闪烁淡入淡出）
    private async Task IgnitePlayer(IReadOnlyList<Creature> targets)
    {
        foreach (var t in targets)
        {
            if (t.IsPlayer)
            {
                global::MegaCrit.Sts2.Core.Commands.VfxCmd.PlayOnCreature(t, "vfx/vfx_fire_burning");
            }
        }
        await Task.CompletedTask;
    }
}

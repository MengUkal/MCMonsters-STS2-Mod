using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：女巫（第3幕普通怪）
/// 被动【炼金术】：玩家单次造成15+伤害 → 向其弃牌堆塞1张灼伤（药水溅射）。
/// 饮用治疗：回复16血，但若上一回合受到20+伤害则被打断（不回复）。
/// 招式循环：喷溅药剂（10+2灼伤）→ 饮用治疗（可打断）→ 毒雾弹（2×8+1灼伤）。
/// 数值：155血（进阶163）。
/// </summary>
public sealed class Witch : MonsterModel
{
    // MC女巫26HP → STS2第3幕 155
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 163, 155);
    public override int MaxInitialHp => MinInitialHp;

    private int SplashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int SplashBurn => 2;
    private int HealAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 16);
    private int HealInterruptThreshold => 20;
    private int CloudDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int CloudHits => 2;
    private int CloudBurn => 1;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<WitchBrewPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // AI循环：喷溅药剂 → 饮用治疗 → 毒雾弹 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState splash = new MoveState("SPLASH", SplashMove, new SingleAttackIntent(SplashDamage), new DebuffIntent());
        MoveState heal = new MoveState("HEAL_POTION", HealMove, new BuffIntent());
        MoveState cloud = new MoveState("POISON_CLOUD", CloudMove, new MultiAttackIntent(CloudDamage, CloudHits), new DebuffIntent());

        splash.FollowUpState = heal;
        heal.FollowUpState = cloud;
        cloud.FollowUpState = splash;

        states.Add(splash);
        states.Add(heal);
        states.Add(cloud);

        return new MonsterMoveStateMachine(states, splash);
    }

    // 喷溅药剂：10 + 2张灼伤
    private async Task SplashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SplashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_witch_throw.mp3")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, SplashBurn, null);
    }

    // 饮用治疗：回16血（上回合受20+伤害则被打断）
    private async Task HealMove(IReadOnlyList<Creature> targets)
    {
        var brew = base.Creature.GetPower<WitchBrewPower>();
        if (brew != null && brew.LastTurnDamage >= HealInterruptThreshold)
        {
            // 被打断：喝药失败，不回复
            await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.3f);
            return;
        }
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_witch_drink.ogg");
        await CreatureCmd.Heal(base.Creature, HealAmount);
    }

    // 毒雾弹：2×8 + 1张灼伤
    private async Task CloudMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CloudDamage).WithHitCount(CloudHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_witch_throw.mp3")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, CloudBurn, null);
    }
}

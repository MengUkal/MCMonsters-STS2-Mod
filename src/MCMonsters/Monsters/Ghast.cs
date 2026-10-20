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
/// 我的世界：恶魂（第2幕精英怪）——完美格挡解谜战
/// 🛡️悬浮庇护：免疫一切伤害（攻击/灼烧/中毒全归零）。
/// 🔒99层人工制品：免疫一切状态效果。
/// 【完美格挡反击】：它的攻击被完美格挡（足额挡下全部伤害）时，恶魂自己受到1点伤害。
/// 3血 = 三次完美格挡即可击败。
/// 意图循环：火球15 → 巨型火球16 → 爆裂火球18。
/// </summary>
public sealed class Ghast : MonsterModel
{
    // 3血——完美格挡3次即胜（进阶4）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 4, 3);
    public override int MaxInitialHp => MinInitialHp;

    private int Attack1Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
    private int Attack2Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);
    private int Attack3Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 18);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 🛡️悬浮庇护：免疫一切伤害
        await PowerCmd.Apply<GhastIntangiblePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        // 【完美格挡反击】：足额格挡它的攻击 → 它-1血
        await PowerCmd.Apply<GhastPerfectBlockPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        // 🔒99层人工制品：免疫一切状态效果
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Creature, 99, Creature, null);
    }

    // AI循环：火球15 → 巨型火球16 → 爆裂火球18 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState a1 = new MoveState("FIREBALL", Attack1Move, new SingleAttackIntent(Attack1Damage));
        MoveState a2 = new MoveState("BIG_FIREBALL", Attack2Move, new SingleAttackIntent(Attack2Damage));
        MoveState a3 = new MoveState("BLAST_FIREBALL", Attack3Move, new SingleAttackIntent(Attack3Damage));

        a1.FollowUpState = a2;
        a2.FollowUpState = a3;
        a3.FollowUpState = a1;

        states.Add(a1);
        states.Add(a2);
        states.Add(a3);

        return new MonsterMoveStateMachine(states, a1);
    }

    // 火球：15
    private async Task Attack1Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Attack1Damage).FromMonster(this)
            .WithAttackerAnim("Cast", 0.3f)
            .WithAttackerFx(null, null, "mc_ghast_attack.ogg")
            .WithHitFx("vfx/fire_impact/vfx_fire_burst_center_flipbook")
            .Execute(null);
        await IgnitePlayer(targets);
    }

    // 巨型火球：16
    private async Task Attack2Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Attack2Damage).FromMonster(this)
            .WithAttackerAnim("Cast", 0.3f)
            .WithAttackerFx(null, null, "mc_ghast_attack.ogg")
            .WithHitFx("vfx/fire_impact/vfx_fire_burst_center_flipbook")
            .Execute(null);
        await IgnitePlayer(targets);
    }

    // 爆裂火球：18
    private async Task Attack3Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Attack3Damage).FromMonster(this)
            .WithAttackerAnim("Cast", 0.3f)
            .WithAttackerFx(null, null, "mc_ghast_attack.ogg")
            .WithHitFx("vfx/fire_impact/vfx_fire_burst_center_flipbook")
            .Execute(null);
        await IgnitePlayer(targets);
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

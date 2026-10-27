using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双凋零骷髅（第2幕池）。下界要塞的组合——凋零之触塞灼伤牌，
/// 双份灼伤抽到就是4点起步，牌库越抽越脏。
/// v1.8.0 起双骷髅错峰起手（复刻一代哨兵）：一只斩击开局、一只防御姿态开局，
/// 任何回合都至多一只举盾。
/// 类名即 ModelId（slug: wither_skeleton_pair）。
/// </summary>
public sealed class WitherSkeletonPair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    // 第2幕弱怪池：双凋零骷髅（2×58血轻压力，凋零debuff温和）
    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<WitherSkeleton>(),
        ModelDb.Monster<WitherSkeleton>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var first = (WitherSkeleton)ModelDb.Monster<WitherSkeleton>().ToMutable();
        var second = (WitherSkeleton)ModelDb.Monster<WitherSkeleton>().ToMutable();
        // 错峰起手：斩击 / 防御姿态（循环顺序不变，只错相位）
        first.StartMoveOffset = 0;
        second.StartMoveOffset = 1;
        return new (MonsterModel, string?)[]
        {
            (first, (string?)null),
            (second, (string?)null)
        };
    }
}

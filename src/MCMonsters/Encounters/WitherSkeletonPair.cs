using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双凋零骷髅（第2幕池）。下界要塞的组合——凋零之触塞灼伤牌，
/// 双份灼伤抽到就是4点起步，牌库越抽越脏。
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
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<WitherSkeleton>().ToMutable(), (string?)null),
            (ModelDb.Monster<WitherSkeleton>().ToMutable(), (string?)null)
        };
    }
}

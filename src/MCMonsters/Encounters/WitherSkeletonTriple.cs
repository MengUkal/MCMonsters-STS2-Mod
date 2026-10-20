using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：三凋零骷髅（第2幕强怪池）。
/// 下界要塞的骷髅小队——三只凋零骷髅轮番斩击，凋零层数叠得飞快。
/// 类名即 ModelId（slug: wither_skeleton_triple）。
/// </summary>
public sealed class WitherSkeletonTriple : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<WitherSkeleton>(),
        ModelDb.Monster<WitherSkeleton>(),
        ModelDb.Monster<WitherSkeleton>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<WitherSkeleton>().ToMutable(), (string?)null),
            (ModelDb.Monster<WitherSkeleton>().ToMutable(), (string?)null),
            (ModelDb.Monster<WitherSkeleton>().ToMutable(), (string?)null)
        };
    }
}

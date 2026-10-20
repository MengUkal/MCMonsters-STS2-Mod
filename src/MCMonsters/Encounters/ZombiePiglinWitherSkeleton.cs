using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：僵尸猪人+凋零骷髅（第2幕强怪池）。
/// 前排蓄怒盾+后排凋零骚扰的经典组合。
/// 类名即 ModelId（slug: zombie_piglin_wither_skeleton）。
/// </summary>
public sealed class ZombiePiglinWitherSkeleton : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<ZombiePiglin>(),
        ModelDb.Monster<WitherSkeleton>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<ZombiePiglin>().ToMutable(), (string?)null),
            (ModelDb.Monster<WitherSkeleton>().ToMutable(), (string?)null)
        };
    }
}

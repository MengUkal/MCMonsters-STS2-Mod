using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：僵尸+骷髅+蜘蛛 三人组（第1幕强怪池）。
/// 僵尸顶前排耗血，骷髅精准箭矢穿格挡，蜘蛛上易伤——三重压力组合。
/// 类名即 ModelId（slug: zombie_skeleton_spider）。
/// </summary>
public sealed class ZombieSkeletonSpider : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Zombie>(),
        ModelDb.Monster<Skeleton>(),
        ModelDb.Monster<Spider>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Zombie>().ToMutable(), (string?)null),
            (ModelDb.Monster<Skeleton>().ToMutable(), (string?)null),
            (ModelDb.Monster<Spider>().ToMutable(), (string?)null)
        };
    }
}

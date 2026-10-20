using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：苦力怕+骷髅 组合（第1幕强怪池）。
/// 骷髅的精准箭矢+苦力怕的3回合全场大爆炸——攻血两头施压。
/// 类名即 ModelId（slug: creeper_skeleton）。
/// </summary>
public sealed class CreeperSkeleton : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Creeper>(),
        ModelDb.Monster<Skeleton>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Creeper>().ToMutable(), (string?)null),
            (ModelDb.Monster<Skeleton>().ToMutable(), (string?)null)
        };
    }
}

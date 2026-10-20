using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双掠夺者（第3幕池）。MC掠夺者前哨站的经典组合——
/// 双弩手轮流装填+散射，节奏完全错开。
/// 类名即 ModelId（slug: pillager_pair）。
/// </summary>
public sealed class PillagerPair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Pillager>(),
        ModelDb.Monster<Pillager>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Pillager>().ToMutable(), (string?)null),
            (ModelDb.Monster<Pillager>().ToMutable(), (string?)null)
        };
    }
}

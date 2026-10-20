using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：掠夺者突袭（第3幕精英）——3个掠夺者+袭击队长。
/// 袭击队长立绘比掠夺者大一圈，意图完全不同（号令全军/狙击/压制）。
/// 类名即 ModelId（slug: pillager_raid_elite）。
/// </summary>
public sealed class PillagerRaidElite : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Pillager>(),
        ModelDb.Monster<Pillager>(),
        ModelDb.Monster<Pillager>(),
        ModelDb.Monster<RaidCaptain>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Pillager>().ToMutable(), (string?)null),
            (ModelDb.Monster<Pillager>().ToMutable(), (string?)null),
            (ModelDb.Monster<Pillager>().ToMutable(), (string?)null),
            (ModelDb.Monster<RaidCaptain>().ToMutable(), (string?)null)
        };
    }
}

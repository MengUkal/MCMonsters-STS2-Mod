using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双末影人（第3幕池）。两只末影人轮流瞬移拉扯——
/// 一个格挡一个背刺，节奏完全错开，非常考验输出规划。
/// 类名即 ModelId（slug: enderman_pair）。
/// </summary>
public sealed class EndermanPair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Enderman>(),
        ModelDb.Monster<Enderman>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Enderman>().ToMutable(), (string?)null),
            (ModelDb.Monster<Enderman>().ToMutable(), (string?)null)
        };
    }
}

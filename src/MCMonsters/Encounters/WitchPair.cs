using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双女巫（第3幕池）。MC沼泽小屋的女巫组合——
/// 双药剂师轮流投掷药剂+自愈，消耗战拉满。
/// 类名即 ModelId（slug: witch_pair）。
/// </summary>
public sealed class WitchPair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Witch>(),
        ModelDb.Monster<Witch>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Witch>().ToMutable(), (string?)null),
            (ModelDb.Monster<Witch>().ToMutable(), (string?)null)
        };
    }
}

using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双末影人。v1.7.0 起不再进入遭遇池（末影人改为单怪机制），
/// 仅保留供控制台 fight enderman_pair 测试。
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

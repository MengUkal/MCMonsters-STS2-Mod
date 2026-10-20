using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：一只苦力怕（MC苦力怕常单独出现在洞穴阴影里）。
/// 类名即 ModelId（slug: creeper_solo）。
/// </summary>
public sealed class CreeperSolo : EncounterModel
{
    private static readonly MonsterModel[] _creepers =
    {
        ModelDb.Monster<Creeper>()
    };

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => _creepers;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Creeper>().ToMutable(), null)
        };
    }
}

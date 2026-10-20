using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双猪灵蛮兵。 extreme难度，仅控制台 fight piglin_brute_pair 测试用，不进池。
/// 类名即 ModelId（slug: piglin_brute_pair）。
/// </summary>
public sealed class PiglinBrutePair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<PiglinBrute>(),
        ModelDb.Monster<PiglinBrute>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<PiglinBrute>().ToMutable(), (string?)null),
            (ModelDb.Monster<PiglinBrute>().ToMutable(), (string?)null)
        };
    }
}

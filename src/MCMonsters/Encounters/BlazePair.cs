using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双烈焰人（MC下界要塞刷怪笼前的经典场面）。
/// 进入第2幕（Hive, Index=1）普通遭遇池。
/// 类名即 ModelId（slug: blaze_pair）。
/// </summary>
public sealed class BlazePair : EncounterModel
{
    private static readonly MonsterModel[] _blazes =
    {
        ModelDb.Monster<Blaze>(),
        ModelDb.Monster<Blaze>()
    };

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => _blazes;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Blaze>().ToMutable(), null),
            (ModelDb.Monster<Blaze>().ToMutable(), null)
        };
    }
}

using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双蜘蛛（MC蜘蛛成群刷在洞穴/夜间，两只交替上易伤很恶心——但可以解）。
/// 类名即 ModelId（slug: spider_pair）。
/// </summary>
public sealed class SpiderPair : EncounterModel
{
    private static readonly MonsterModel[] _spiders =
    {
        ModelDb.Monster<Spider>(),
        ModelDb.Monster<Spider>()
    };

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => _spiders;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Spider>().ToMutable(), null),
            (ModelDb.Monster<Spider>().ToMutable(), null)
        };
    }
}


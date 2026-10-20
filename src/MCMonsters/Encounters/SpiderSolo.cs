using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：单只蜘蛛。不进遭遇池（单只难度不足），仅保留供控制台 fight spider_solo 测试。
/// </summary>
public sealed class SpiderSolo : EncounterModel
{
    private static readonly MonsterModel[] _spiders =
    {
        ModelDb.Monster<Spider>()
    };

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => _spiders;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Spider>().ToMutable(), null)
        };
    }
}

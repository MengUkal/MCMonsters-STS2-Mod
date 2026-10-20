using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：巨人（第3幕精英）。
/// RoomType.Elite → AllEliteEncounters 自动从 AllEncounters 过滤进精英池
/// （第3幕精英池：恶魂 / 巨人）。
/// 类名即 ModelId（slug: giant_elite）。
/// </summary>
public sealed class GiantElite : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<Giant>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<Giant>().ToMutable(), (string?)null) };
    }
}

using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双潜影贝（第3幕池）。MC末地城的潜影贝成排附着——
/// 双贝壳轮流闭合防御+弹幕，消耗战拉满。
/// 类名即 ModelId（slug: shulker_pair）。
/// </summary>
public sealed class ShulkerPair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Shulker>(),
        ModelDb.Monster<Shulker>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Shulker>().ToMutable(), (string?)null),
            (ModelDb.Monster<Shulker>().ToMutable(), (string?)null)
        };
    }
}

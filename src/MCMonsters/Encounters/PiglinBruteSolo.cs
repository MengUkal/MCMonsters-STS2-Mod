using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：单只猪灵蛮兵（第2幕池，小型精英定位——第2幕最肉普通怪，单人并不简单）。
/// 类名即 ModelId（slug: piglin_brute_solo）。
/// </summary>
public sealed class PiglinBruteSolo : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<PiglinBrute>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<PiglinBrute>().ToMutable(), (string?)null) };
    }
}

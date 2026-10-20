using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：恶魂（第2幕精英）。
/// RoomType.Elite → 自动进第2幕精英池。
/// HasScene=true + ghast槽位标记抬高（悬浮在空中），见 scenes/encounters/ghast_elite.tscn。
/// 类名即 ModelId（slug: ghast_elite）。
/// </summary>
public sealed class GhastElite : EncounterModel
{
    private const string GhastSlot = "ghast";

    public override RoomType RoomType => RoomType.Elite;

    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[] { GhastSlot };

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<Ghast>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<Ghast>().ToMutable(), GhastSlot) };
    }
}

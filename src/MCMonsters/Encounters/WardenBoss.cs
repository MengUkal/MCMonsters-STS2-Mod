using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：监守者（第1幕BOSS）。
/// RoomType.Boss → AllBossEncounters 自动从 AllEncounters 过滤进BOSS池
/// （第1幕BOSS池：与官方BOSS随机出现）。
/// 类名即 ModelId（slug: warden_boss）。
/// </summary>
public sealed class WardenBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<Warden>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<Warden>().ToMutable(), (string?)null) };
    }
}

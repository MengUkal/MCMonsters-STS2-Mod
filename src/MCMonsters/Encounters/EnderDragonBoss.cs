using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：末影龙（第3幕BOSS，三形态600总血）。
/// RoomType.Boss → AllBossEncounters 自动从 AllEncounters 过滤进BOSS池
/// （与官方 AeonglassBoss/TestSubjectBoss 等随机出现）。
/// HasScene=true：dragon槽位高抬（末影龙飞行在空中），场景 scenes/encounters/ender_dragon_boss.tscn。
/// 类名即 ModelId（slug: ender_dragon_boss）。
/// </summary>
public sealed class EnderDragonBoss : EncounterModel
{
    private const string DragonSlot = "dragon";

    public override RoomType RoomType => RoomType.Boss;

    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[] { DragonSlot };

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<EnderDragon>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<EnderDragon>().ToMutable(), DragonSlot) };
    }
}

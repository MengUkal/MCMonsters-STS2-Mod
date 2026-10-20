using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：凋零（第2幕BOSS，三形态330总血）。
/// RoomType.Boss → AllBossEncounters 自动从 AllEncounters 过滤进BOSS池（与官方BOSS随机三选一）。
/// 注意：控制台 `fight wither_boss_encounter` 对 Boss 类型无效（fight指令硬编码Monster房间），
/// 但地图BOSS节点会正常出现本遭遇。
/// HasScene=true：wither槽位抬高（凋零悬浮），场景 scenes/encounters/wither_boss_encounter.tscn
/// （场景文件名必须是遭遇Id全称，见踩坑记录.md 坑2）。
/// 类名即 ModelId（slug: wither_boss_encounter）。
/// </summary>
public sealed class WitherBossEncounter : EncounterModel
{
    private const string WitherSlot = "wither";

    public override RoomType RoomType => RoomType.Boss;

    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[] { WitherSlot };

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<WitherBoss>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<WitherBoss>().ToMutable(), WitherSlot) };
    }
}

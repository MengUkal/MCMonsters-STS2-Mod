using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：烈焰人+凋零骷髅（第2幕弱怪池）。
/// 空中火焰+地面凋零的组合——灼伤牌与凋零层数同时施压。
/// 类名即 ModelId（slug: blaze_wither_skeleton）。
/// </summary>
public sealed class BlazeWitherSkeleton : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    // 第2幕弱怪池：74+58=132合计血量
    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Blaze>(),
        ModelDb.Monster<WitherSkeleton>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Blaze>().ToMutable(), (string?)null),
            (ModelDb.Monster<WitherSkeleton>().ToMutable(), (string?)null)
        };
    }
}

using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：烈焰人+僵尸猪人（第2幕强怪池）。
/// 远程灼伤+近战蓄怒——僵尸猪人中立时蓄怒，烈焰人持续塞灼伤牌。
/// 类名即 ModelId（slug: blaze_zombie_piglin）。
/// </summary>
public sealed class BlazeZombiePiglin : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Blaze>(),
        ModelDb.Monster<ZombiePiglin>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Blaze>().ToMutable(), (string?)null),
            (ModelDb.Monster<ZombiePiglin>().ToMutable(), (string?)null)
        };
    }
}

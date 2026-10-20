using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双僵尸猪人（第2幕池）。MC猪人成群，打一只全体激怒——
/// 同仇敌忾×2，越打越硬的经典猪人体验。
/// 类名即 ModelId（slug: zombie_piglin_pair）。
/// </summary>
public sealed class ZombiePiglinPair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<ZombiePiglin>(),
        ModelDb.Monster<ZombiePiglin>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<ZombiePiglin>().ToMutable(), (string?)null),
            (ModelDb.Monster<ZombiePiglin>().ToMutable(), (string?)null)
        };
    }
}

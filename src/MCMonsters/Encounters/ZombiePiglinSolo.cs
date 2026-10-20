using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：单只僵尸猪人。不进遭遇池，仅保留供控制台 fight zombie_piglin_solo 测试。
/// </summary>
public sealed class ZombiePiglinSolo : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<ZombiePiglin>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<ZombiePiglin>().ToMutable(), (string?)null) };
    }
}

using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：单只末影人。v1.7.0 起进入第3幕遭遇池
/// （末影人的中立激怒机制只适合单怪出现）。
/// </summary>
public sealed class EndermanSolo : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] { ModelDb.Monster<Enderman>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[] { (ModelDb.Monster<Enderman>().ToMutable(), (string?)null) };
    }
}

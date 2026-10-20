using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：小僵尸鸡骑士（第1幕精英，双命）。
/// RoomType.Elite → ActModel.AllEliteEncounters 自动从 AllEncounters 过滤进精英池。
/// Slots 提供召唤槽位：鸡骑士本体占 jockey 槽，开场召唤的僵尸/骷髅占 minion1/2 槽
/// （不提供空槽位时 EncounterModel.GetNextSlot 拿不到槽，召唤会静默失败）。
/// </summary>
public sealed class ChickenJockeyElite : EncounterModel
{
    private const string JockeySlot = "jockey";
    private const string MinionSlot1 = "minion1";
    private const string MinionSlot2 = "minion2";

    private static readonly MonsterModel[] _monsters =
    {
        ModelDb.Monster<ChickenJockey>()
    };

    public override RoomType RoomType => RoomType.Elite;

    public override bool HasScene => true;

    public override IReadOnlyList<string> Slots => new[] { JockeySlot, MinionSlot1, MinionSlot2 };

    public override IEnumerable<MonsterModel> AllPossibleMonsters => _monsters;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<ChickenJockey>().ToMutable(), JockeySlot)
        };
    }
}

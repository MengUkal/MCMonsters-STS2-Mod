using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：单只烈焰人。不进遭遇池，仅保留供控制台 fight blaze_solo 测试。
/// </summary>
public sealed class BlazeSolo : EncounterModel
{
    private static readonly MonsterModel[] _blazes =
    {
        ModelDb.Monster<Blaze>()
    };

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => _blazes;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Blaze>().ToMutable(), null)
        };
    }
}

using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：一只僵尸（普通怪，非weak池）。
/// 类名即 ModelId（slug: zombie_solo），无需手动注册——
/// ModelDb.Init() 会扫描MOD程序集中所有 EncounterModel 子类自动注册。
/// </summary>
public sealed class ZombieSolo : EncounterModel
{
    private static readonly MonsterModel[] _zombies =
    {
        ModelDb.Monster<Zombie>()
    };

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => _zombies;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<Zombie>().ToMutable(), null)
        };
    }
}


using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：双潜影贝（第3幕池）。MC末地城的潜影贝成排附着——
/// 双贝壳起手错开：一只以闭合防御开始，一只以开壳弹幕开始，
/// 之后各自闭合/开壳交替，消耗战拉满。
/// 类名即 ModelId（slug: shulker_pair）。
/// </summary>
public sealed class ShulkerPair : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<Shulker>(),
        ModelDb.Monster<Shulker>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var first = (Shulker)ModelDb.Monster<Shulker>().ToMutable();
        var second = (Shulker)ModelDb.Monster<Shulker>().ToMutable();
        // 两只初始意图必为一格挡一攻击：随机谁先开壳（遭遇级RNG，同种子可复现）
        bool firstBarrage = Rng.NextBool();
        first.StartOnBarrage = firstBarrage;
        second.StartOnBarrage = !firstBarrage;
        return new (MonsterModel, string?)[]
        {
            (first, (string?)null),
            (second, (string?)null)
        };
    }
}

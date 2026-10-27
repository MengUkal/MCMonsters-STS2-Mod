using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MCMonsters.Monsters;

namespace MCMonsters.Encounters;

/// <summary>
/// 遭遇：三凋零骷髅（第2幕强怪池）。
/// 下界要塞的骷髅小队——三只凋零骷髅轮番斩击，凋零层数叠得飞快。
/// v1.8.0 起三骷髅错峰起手（复刻一代哨兵的轮转错位）：
/// 分别以斩击/防御姿态/重击开局，之后各自循环节奏不变，
/// 任何回合都是一攻一防一重击，不再全体同回合举盾。
/// 类名即 ModelId（slug: wither_skeleton_triple）。
/// </summary>
public sealed class WitherSkeletonTriple : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<WitherSkeleton>(),
        ModelDb.Monster<WitherSkeleton>(),
        ModelDb.Monster<WitherSkeleton>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var first = (WitherSkeleton)ModelDb.Monster<WitherSkeleton>().ToMutable();
        var second = (WitherSkeleton)ModelDb.Monster<WitherSkeleton>().ToMutable();
        var third = (WitherSkeleton)ModelDb.Monster<WitherSkeleton>().ToMutable();
        // 错峰起手：斩击 / 防御姿态 / 重击（循环顺序不变，只错相位）
        first.StartMoveOffset = 0;
        second.StartMoveOffset = 1;
        third.StartMoveOffset = 2;
        return new (MonsterModel, string?)[]
        {
            (first, (string?)null),
            (second, (string?)null),
            (third, (string?)null)
        };
    }
}

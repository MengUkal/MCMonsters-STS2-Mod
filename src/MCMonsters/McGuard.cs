using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;

namespace MCMonsters;

/// <summary>
/// 招式护栏：把每个怪物招式的执行体包一层 try/catch。
///
/// 为什么需要：StS2 的敌方回合是"按怪物顺序逐个 await 招式"的。任何一只怪的招式里抛出异常，
/// 异常会顺着 MonsterMoveStateMachine 的任务链冒泡，导致：
///   · 该招式后面的逻辑（例如附带的掉血 / 上 debuff）不执行；
///   · 后续怪物永远不行动；
///   · 玩家看到的回合永远不结束（牌停在屏幕上）。
/// 招式体本身是 mod 代码，所以这里兜住异常、写日志（含完整栈），让敌方回合能继续推进。
/// 这不是"掩盖 bug"：异常会以 [MCMonsters] move 'X' failed 的形式完整记录在 godot.log 里。
/// </summary>
public static class McGuard
{
    /// <summary>Wraps a monster move so a throw cannot break the enemy turn.</summary>
    public static Func<IReadOnlyList<Creature>, Task> Move(
        string moveId,
        Func<IReadOnlyList<Creature>, Task> body)
    {
        return async targets =>
        {
            try
            {
                await body(targets);
            }
            catch (Exception ex)
            {
                Log.Warn($"[{ModEntry.ModId}] move '{moveId}' failed (enemy turn continues): {ex}");
            }
        };
    }

    /// <summary>Runs an arbitrary async step, logging instead of throwing.</summary>
    public static async Task Run(string label, Func<Task> body)
    {
        try
        {
            await body();
        }
        catch (Exception ex)
        {
            Log.Warn($"[{ModEntry.ModId}] '{label}' failed: {ex}");
        }
    }
}

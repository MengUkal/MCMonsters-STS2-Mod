using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MCMonsters.Powers;

namespace MCMonsters.DevCommands;

/// <summary>
/// TEMPORARY debug commands used to reproduce the reported "Wither never resolves" bug in-game.
/// The in-game End Turn button does not accept synthetic clicks from automation, so this lets us
/// advance turns deterministically and stress the Wither power hooks.
/// Remove before release.
/// </summary>
public sealed class McDebugConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "mcend";

    public override string Args => "[times:int]";

    public override string Description => "DEBUG: end the local player's turn N times (default 1).";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        CombatState? state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null)
        {
            return new CmdResult(success: false, "not in combat");
        }
        Player? me = issuingPlayer ?? LocalContext.GetMe(state);
        if (me == null)
        {
            return new CmdResult(success: false, "no local player");
        }
        int times = 1;
        if (args.Length > 0 && int.TryParse(args[0], out int parsed) && parsed > 0)
        {
            times = parsed;
        }
        for (int i = 0; i < times; i++)
        {
            PlayerCmd.EndTurn(me, canBackOut: false);
        }
        return new CmdResult(success: true, $"end turn x{times}");
    }
}

/// <summary>DEBUG: apply the mod's Wither debuff to the player (for turn-start tick testing).</summary>
public sealed class McWitherDebugCmd : AbstractConsoleCmd
{
    public override string CmdName => "mcwither";

    public override string Args => "[amount:int]";

    public override string Description => "DEBUG: apply MCMonsters Wither to the local player.";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        CombatState? state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null)
        {
            return new CmdResult(success: false, "not in combat");
        }
        Player? me = issuingPlayer ?? LocalContext.GetMe(state);
        if (me == null)
        {
            return new CmdResult(success: false, "no local player");
        }
        int amount = 2;
        if (args.Length > 0 && int.TryParse(args[0], out int parsed) && parsed > 0)
        {
            amount = parsed;
        }
        var task = PowerCmd.Apply<McWitherPower>(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
            me.Creature, amount, me.Creature, null);
        return new CmdResult((System.Threading.Tasks.Task)task, success: true, $"wither +{amount}");
    }
}

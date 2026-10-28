using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MCMonsters.Monsters;

namespace MCMonsters.DevTools;

/// <summary>
/// ⚠️ 临时调试命令（复现凋灵 99 人工制品后攻击浮空问题）——发布前必须删除！
/// mcfight &lt;encounter_id&gt; : 跳遭遇（尊重遭遇自身 RoomType，BOSS 也能跳）
/// mcwither                 : 脚本化复现——把凋灵打进半血（触发 99 人工制品），再程序化出一张攻击牌打它，观察牌是否浮空
/// </summary>
public sealed class McWitherDebugCmd : AbstractConsoleCmd
{
    public override string CmdName => "mcwither";

    public override string Args => "";

    public override string Description => "[TEMP] Repro: push Wither below half HP then play an attack at it.";

    public override bool IsNetworked => false;

    // DebugOnly=false：0.107.1 正式版控制台也能注册（默认 DebugOnly=true 的命令会被过滤）
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (!CombatManager.Instance.IsInProgress)
        {
            return new CmdResult(success: false, "Not in combat.");
        }
        CombatState combatState = CombatManager.Instance.DebugOnlyGetState();
        Creature? wither = combatState.Enemies.FirstOrDefault((Creature e) => e.Monster is WitherBoss)
            ?? combatState.Enemies.FirstOrDefault();
        if (wither == null)
        {
            return new CmdResult(success: false, "No enemy found.");
        }
        Player? player = combatState.Players.FirstOrDefault();
        if (player == null)
        {
            return new CmdResult(success: false, "No player found.");
        }
        Task task = RunAsync(combatState, player, wither);
        return new CmdResult(task, success: true, $"mcwither repro started (wither {wither.CurrentHp}/{wither.MaxHp}).");
    }

    private static async Task RunAsync(CombatState combatState, Player player, Creature wither)
    {
        try
        {
            Log.Info($"[MCMonsters][repro] === start: wither {wither.CurrentHp}/{wither.MaxHp}");

            // 1) 穿过真实伤害管线把凋灵打到半血以下（触发 AfterDamageGiven 钩子 → 99 人工制品）
            decimal dmg = wither.CurrentHp - (wither.MaxHp / 2m) + 5m;
            if (dmg < 1m)
            {
                dmg = 1m;
            }
            Log.Info($"[MCMonsters][repro] dealing {dmg} via CreatureCmd.Damage (BlockingPlayerChoiceContext)");
            await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), new List<Creature> { wither }, dmg, ValueProp.Unpowered, null, null);
            Log.Info($"[MCMonsters][repro] after damage: wither {wither.CurrentHp}/{wither.MaxHp}, powers: {DescribePowers(wither)}");

            // 2) 确保手牌里有一张攻击牌
            CardPile hand = PileType.Hand.GetPile(player);
            CardModel? attack = hand.Cards.FirstOrDefault((CardModel c) => c.Type == CardType.Attack);
            if (attack == null)
            {
                CardModel template = ModelDb.AllCards.First((CardModel c) => c.Type == CardType.Attack && c.Id.Entry.Contains("STRIKE"));
                attack = combatState.CreateCard(template, player);
                await CardPileCmd.Add(attack, PileType.Hand);
                Log.Info($"[MCMonsters][repro] spawned attack card {attack.Id.Entry} into hand");
            }
            else
            {
                Log.Info($"[MCMonsters][repro] using hand attack card {attack.Id.Entry}");
            }

            // 3) 能量管够
            await PlayerCmd.GainEnergy(10, player);

            // 4) 走真实出牌队列打凋灵
            Log.Info($"[MCMonsters][repro] enqueueing PlayCardAction {attack.Id.Entry} -> wither");
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new PlayCardAction(attack, wither));

            // 5) 等它结算：最多 8 秒，每 200ms 检查牌是否离开手牌
            for (int i = 0; i < 40; i++)
            {
                await Task.Delay(200);
                CardPile? pile = attack.Pile;
                if (pile != null && pile.Type != PileType.Hand)
                {
                    break;
                }
            }
            CardPile? finalPile = attack.Pile;
            bool floating = finalPile == null || finalPile.Type == PileType.Hand;
            Log.Info($"[MCMonsters][repro] RESULT: card pile={(finalPile?.Type.ToString() ?? "null")} floating={floating}, wither HP {wither.CurrentHp}/{wither.MaxHp}, powers: {DescribePowers(wither)}");
            Log.Info("[MCMonsters][repro] === end");
        }
        catch (Exception ex)
        {
            Log.Error($"[MCMonsters][repro] EXCEPTION: {ex}");
        }
    }

    private static string DescribePowers(Creature creature)
    {
        return string.Join(",", creature.Powers.Select((PowerModel p) => p.Id.Entry + ":" + p.Amount));
    }
}

/// <summary>
/// ⚠️ 临时调试命令——发布前必须删除！
/// mcfight &lt;encounter_id&gt;：与官方 fight 相同，但把遭遇自身的 RoomType 传给 EnterRoomDebug，
/// 这样 BOSS 遭遇（如 wither_boss_encounter）也能直接跳（官方 fight 写死 Monster 房间 → BOSS 跳不进去）。
/// </summary>
public sealed class McFightDebugCmd : AbstractConsoleCmd
{
    public override string CmdName => "mcfight";

    public override string Args => "<id:string>";

    public override string Description => "[TEMP] Jump to an encounter using its own room type (boss works).";

    public override bool IsNetworked => false;

    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0)
        {
            return new CmdResult(success: false, "No encounter name specified.");
        }
        if (!RunManager.Instance.IsInProgress)
        {
            return new CmdResult(success: false, "A run is currently not in progress!");
        }
        ModelId modelId = new ModelId(ModelId.SlugifyCategory<EncounterModel>(), args[0].ToUpperInvariant());
        EncounterModel encounterModel;
        try
        {
            encounterModel = ModelDb.GetById<EncounterModel>(modelId).ToMutable();
        }
        catch (Exception)
        {
            return new CmdResult(success: false, "Encounter '" + modelId.Entry + "' not found");
        }
        encounterModel.DebugRandomizeRng();
        Task task = RunManager.Instance.EnterRoomDebug(encounterModel.RoomType, MapPointType.Unassigned, encounterModel);
        return new CmdResult(task, success: true, "Jumped to encounter: '" + encounterModel.Id.Entry + "' (" + encounterModel.RoomType + ")");
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyFirstCharacter.MyFirstCharacterCode.Powers;

namespace MyFirstCharacter.MyFirstCharacterCode.FGOCore;

/// <summary>
/// NP 追踪器。NP 按 Player（局内持久）存储，跨战斗继承，上限 300；显示条同步到 NpDisplayPower。
/// </summary>
public static class NpCharge
{
    private static readonly Dictionary<Player, int> _np = new();
    private static readonly Dictionary<Creature, int> _npCardsPlayed = new();
    private static readonly Dictionary<Creature, int> _switchCount = new();

    /// <summary>Fires when NP crosses from below 100 to >= 100. Subscribe to auto-generate NP cards.</summary>
    public static event Func<Creature, Task>? GaugeFilled;

    private static Player Key(Creature creature) => creature.Player!;

    public static int Current(Creature creature) => _np.GetValueOrDefault(Key(creature), 0);
    public static bool IsOvercharged(Creature creature) => Current(creature) >= 100;

    /// <summary>同步 NP 显示条（战斗开始时调用可恢复上一场的 NP 显示）。</summary>
    public static async Task SyncDisplay(Creature creature)
    {
        int cur = Current(creature);
        var power = creature.GetPower<NpDisplayPower>();
        if (power != null)
        {
            if (cur > 0)
                await PowerCmd.ModifyAmount(new BlockingPlayerChoiceContext(), power, cur - power.Amount, creature, null, false);
            else
                await PowerCmd.Remove(power);
        }
        else if (cur > 0)
        {
            await PowerCmd.Apply<NpDisplayPower>(new BlockingPlayerChoiceContext(), creature, cur, creature, null, false);
        }
    }

    public static async Task Gain(Creature creature, int amount)
    {
        int cur = Current(creature);
        bool wasBelow = cur < 100;
        _np[Key(creature)] = Math.Clamp(cur + amount, 0, 300);
        await SyncDisplay(creature);

        // Fire event when NP crosses threshold
        if (wasBelow && IsOvercharged(creature) && GaugeFilled != null)
        {
            foreach (Func<Creature, Task> handler in GaugeFilled.GetInvocationList())
                await handler(creature);
        }
    }

    public static void GainSync(Creature creature, int amount)
    {
        int cur = Current(creature);
        _np[Key(creature)] = Math.Clamp(cur + amount, 0, 300);
        _ = SyncDisplay(creature);
    }

    public static bool CanPay(Creature creature, int amount) => Current(creature) >= amount;

    public static async Task<bool> Spend(Creature creature, int amount)
    {
        int cur = Current(creature);
        if (cur < amount) return false;
        _np[Key(creature)] = cur - amount;
        await SyncDisplay(creature);
        return true;
    }

    public static async Task<int> ConsumeAllForNpCard(Creature creature, int minCost)
    {
        int cur = Current(creature);
        int spend = Math.Max(cur, minCost);
        _np[Key(creature)] = 0;
        await SyncDisplay(creature);
        return spend;
    }

    /// <summary>本场战斗打出的宝具卡次数。</summary>
    public static int NpCardsPlayed(Creature creature) => _npCardsPlayed.GetValueOrDefault(creature, 0);

    public static void IncrementNpCardsPlayed(Creature creature) => _npCardsPlayed[creature] = _npCardsPlayed.GetValueOrDefault(creature, 0) + 1;

    /// <summary>本场战斗切换形态的次数。</summary>
    public static int SwitchCount(Creature creature) => _switchCount.GetValueOrDefault(creature, 0);

    public static void IncrementSwitchCount(Creature creature) => _switchCount[creature] = _switchCount.GetValueOrDefault(creature, 0) + 1;

    /// <summary>战斗开始时重置每场战斗的计数（宝具卡次数、切换次数），NP 本体保留。</summary>
    public static void ResetCombat(Creature creature)
    {
        _npCardsPlayed.Remove(creature);
        _switchCount.Remove(creature);
    }
}

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

    /// <summary>宝具值每次变化后触发（增加 / 消耗 / 清零都算）。订阅它来自动补宝具卡。</summary>
    public static event Func<Creature, Task>? NpChanged;

    /// <summary>跨战斗最多能带进下一场的宝具值。</summary>
    public const int CarriedNpCap = 100;

    /// <summary>整局开局一次性给的宝具值。</summary>
    public const int StartingNp = 50;

    private static async Task NotifyChanged(Creature creature)
    {
        if (NpChanged == null) return;
        foreach (Func<Creature, Task> handler in NpChanged.GetInvocationList())
            await handler(creature);
    }

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
        _np[Key(creature)] = Math.Clamp(Current(creature) + amount, 0, 300);
        await SyncDisplay(creature);
        await NotifyChanged(creature);
    }

    public static void GainSync(Creature creature, int amount)
    {
        _np[Key(creature)] = Math.Clamp(Current(creature) + amount, 0, 300);
        _ = SyncDisplay(creature);
        _ = NotifyChanged(creature);
    }

    public static bool CanPay(Creature creature, int amount) => Current(creature) >= amount;

    public static async Task<bool> Spend(Creature creature, int amount)
    {
        int cur = Current(creature);
        if (cur < amount) return false;
        _np[Key(creature)] = cur - amount;
        await SyncDisplay(creature);
        await NotifyChanged(creature);
        return true;
    }

    public static async Task<int> ConsumeAllForNpCard(Creature creature, int minCost)
    {
        int cur = Current(creature);
        int spend = Math.Max(cur, minCost);
        _np[Key(creature)] = 0;
        await SyncDisplay(creature);
        await NotifyChanged(creature);
        return spend;
    }

    /// <summary>本场战斗打出的宝具卡次数。</summary>
    public static int NpCardsPlayed(Creature creature) => _npCardsPlayed.GetValueOrDefault(creature, 0);

    public static void IncrementNpCardsPlayed(Creature creature) => _npCardsPlayed[creature] = _npCardsPlayed.GetValueOrDefault(creature, 0) + 1;

    /// <summary>本场战斗切换形态的次数。</summary>
    public static int SwitchCount(Creature creature) => _switchCount.GetValueOrDefault(creature, 0);

    public static void IncrementSwitchCount(Creature creature) => _switchCount[creature] = _switchCount.GetValueOrDefault(creature, 0) + 1;

    /// <summary>
    /// 战斗开始时调用：
    ///   - 本局第一次战斗 -> 给开局宝具值 50 点；
    ///   - 之后每场 -> 把上一场带过来的宝具值压到不超过 100 点（战斗内可以超过 100，只是带不走）。
    /// 另外清掉每场战斗的计数（宝具卡次数、切换次数）。
    /// </summary>
    public static async Task BeginCombat(Creature creature)
    {
        var key = Key(creature);
        if (!_np.ContainsKey(key))
            _np[key] = StartingNp;
        else if (_np[key] > CarriedNpCap)
            _np[key] = CarriedNpCap;

        _npCardsPlayed.Remove(creature);
        _switchCount.Remove(creature);
        await SyncDisplay(creature);
    }
}

using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyFirstCharacter.MyFirstCharacterCode.Powers;
using MyFirstCharacter.MyFirstCharacterCode.Relics;

namespace MyFirstCharacter.MyFirstCharacterCode.FGOCore;

/// <summary>
/// NP 追踪器。NP 按 Player（局内持久）存储，跨战斗继承，上限 300；显示条同步到 NpDisplayPower。
///
/// 真实存储挂在载体遗物上（见 <see cref="NpFields"/>），所以它会随存档和多人权威快照一起走。
/// 静态字典降级为「玩家身上找不到载体遗物时」的兜底镜像 —— 那种情况下不再同步，但不崩。
/// </summary>
public static class NpCharge
{
    private const int MaxNp = 300;

    /// <summary>宝具值每次变化后触发（增加 / 消耗 / 清零都算）。订阅它来自动补宝具卡。
    /// <para>
    /// 只由本地的 <see cref="Gain"/> / <see cref="Spend"/> 触发。**导入远端快照、遗物迁移、镜像刷新
    /// 一律只写原始值、绝不触发** —— 否则会把一次快照导入当成一次「获得」，重复往手牌补宝具卡。
    /// </para></summary>
    public static event Func<Creature, Task>? NpChanged;

    /// <summary>跨战斗最多能带进下一场的宝具值。</summary>
    public const int CarriedNpCap = 100;

    /// <summary>整局开局一次性给的宝具值。</summary>
    public const int StartingNp = 50;

    // 兜底存储：只在玩家身上找不到载体遗物时使用（遗物被事件移除、别的角色拿到我们的卡）。
    // 有遗物时它只是镜像；没有遗物时它就是唯一的来源。
    private static readonly Dictionary<Player, int> _fbNp = new();
    private static readonly Dictionary<Player, int> _fbNpCards = new();
    private static readonly Dictionary<Player, int> _fbSwitch = new();

    private static async Task NotifyChanged(Creature creature)
    {
        if (NpChanged == null) return;
        foreach (Func<Creature, Task> handler in NpChanged.GetInvocationList())
            await handler(creature);
    }

    private static Player Key(Creature creature) => creature.Player!;

    /// <summary>遗物优先（值不是 Unset 就算数），否则退回兜底字典，都没有则返回 Unset。</summary>
    private static int Read(Creature creature, SavedSpireField<MyFirstCharacterRelic, int> field,
                            Dictionary<Player, int> fallback)
    {
        Player player = Key(creature);
        MyFirstCharacterRelic? relic = NpRelic.Carrier(player);
        if (relic != null)
        {
            int fromRelic = field[relic];
            if (fromRelic != NpFields.Unset) return fromRelic;
        }
        return fallback.TryGetValue(player, out int fromFallback) ? fromFallback : NpFields.Unset;
    }

    /// <summary>同时写遗物和兜底字典 —— 镜像保住，万一遗物没了也只是降级，不会跳回 0。</summary>
    private static void Write(Creature creature, SavedSpireField<MyFirstCharacterRelic, int> field,
                              Dictionary<Player, int> fallback, int value)
    {
        Player player = Key(creature);
        fallback[player] = value;
        MyFirstCharacterRelic? relic = NpRelic.Carrier(player);
        if (relic != null) field[relic] = value;
    }

    public static int Current(Creature creature)
    {
        int value = Read(creature, NpFields.Np, _fbNp);
        return value == NpFields.Unset ? 0 : value;
    }

    public static bool IsOvercharged(Creature creature) => Current(creature) >= 100;

    /// <summary>临时诊断：区分「载体遗物没找到」「遗物字段没被导入」「值被压到带入上限」。
    /// 复现存档往返问题后从 godot.log 里 grep `[NP]` 即可。</summary>
    private static void LogState(Creature creature, int rawRead)
    {
        Player player = Key(creature);
        MyFirstCharacterRelic? relic = NpRelic.Carrier(player);
        string relicPart = relic == null
            ? "载体=无"
            : $"载体={relic.Id.Entry} 遗物np={NpFields.Np[relic]} 遗物卡数={NpFields.NpCardsPlayed[relic]} 遗物切形={NpFields.SwitchCount[relic]}";
        MainFile.Logger.Info($"[NP] 战斗开始 netId={player.NetId} {relicPart} "
            + $"兜底np={(_fbNp.TryGetValue(player, out int fb) ? fb : -999)} 读到={rawRead}");
    }

    /// <summary>同步 NP 显示条（战斗开始时调用可恢复上一场的 NP 显示）。</summary>
    public static async Task SyncDisplay(Creature creature)
    {
        int cur = Current(creature);
        var power = creature.GetPower<NpDisplayPower>();
        int before = power?.Amount ?? -999;
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
        // 临时诊断：显示条实际被设成了多少（NpDisplayPower 的 Amount 就是玩家看到的数字）
        MainFile.Logger.Info($"[NP] 显示条 netId={creature.Player?.NetId} 原={before} 设成={cur}");
    }

    public static async Task Gain(Creature creature, int amount)
    {
        Write(creature, NpFields.Np, _fbNp, Math.Clamp(Current(creature) + amount, 0, MaxNp));
        await SyncDisplay(creature);
        await NotifyChanged(creature);
    }

    public static bool CanPay(Creature creature, int amount) => Current(creature) >= amount;

    public static async Task<bool> Spend(Creature creature, int amount)
    {
        int cur = Current(creature);
        if (cur < amount) return false;
        Write(creature, NpFields.Np, _fbNp, cur - amount);
        await SyncDisplay(creature);
        await NotifyChanged(creature);
        return true;
    }

    /// <summary>本场战斗打出的宝具卡次数。</summary>
    public static int NpCardsPlayed(Creature creature)
    {
        int value = Read(creature, NpFields.NpCardsPlayed, _fbNpCards);
        return value == NpFields.Unset ? 0 : value;
    }

    public static void IncrementNpCardsPlayed(Creature creature)
        => Write(creature, NpFields.NpCardsPlayed, _fbNpCards, NpCardsPlayed(creature) + 1);

    /// <summary>本场战斗切换形态的次数。</summary>
    public static int SwitchCount(Creature creature)
    {
        int value = Read(creature, NpFields.SwitchCount, _fbSwitch);
        return value == NpFields.Unset ? 0 : value;
    }

    public static void IncrementSwitchCount(Creature creature)
        => Write(creature, NpFields.SwitchCount, _fbSwitch, SwitchCount(creature) + 1);

    /// <summary>
    /// 战斗开始时调用：
    ///   - 本局第一次战斗 -> 给开局宝具值 50 点；
    ///   - 之后每场 -> 把上一场带过来的宝具值压到不超过 100 点（战斗内可以超过 100，只是带不走）。
    /// 另外清掉每场战斗的计数（宝具卡次数、切换次数）。
    /// </summary>
    public static async Task BeginCombat(Creature creature)
    {
        int cur = Read(creature, NpFields.Np, _fbNp);
        LogState(creature, cur);
        if (cur == NpFields.Unset) Write(creature, NpFields.Np, _fbNp, StartingNp);
        else if (cur > CarriedNpCap) Write(creature, NpFields.Np, _fbNp, CarriedNpCap);

        Write(creature, NpFields.NpCardsPlayed, _fbNpCards, 0);
        Write(creature, NpFields.SwitchCount, _fbSwitch, 0);

        NpRelic.ClearStash(Key(creature));
        // HashSet<Creature> 持强引用且从不清，长局里会慢慢堆积；每场战斗清一次就够。
        TurnTracker.ClearExhausted(creature);

        await SyncDisplay(creature);
    }

    /// <summary>
    /// 把遗物上的值抄进兜底字典。远端快照落地后调用（见 Patches/NpSyncPatches）——
    /// 否则本地那份镜像会停在导入前的旧值，而玩家一旦丢了载体遗物就会读到旧值。
    /// **不触发 NpChanged**，也不碰 NpDisplayPower（同步永远发生在战斗外，那时 Creature 上没有战斗 power）。
    /// </summary>
    internal static void RefreshMirror(Player player)
    {
        MyFirstCharacterRelic? relic = NpRelic.Carrier(player);
        if (relic == null) return;
        _fbNp[player] = NpFields.Np[relic];
        _fbNpCards[player] = NpFields.NpCardsPlayed[relic];
        _fbSwitch[player] = NpFields.SwitchCount[relic];
    }
}

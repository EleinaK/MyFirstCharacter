using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MyFirstCharacter.MyFirstCharacterCode.Relics;

namespace MyFirstCharacter.MyFirstCharacterCode.FGOCore;

/// <summary>
/// Player / Creature → 宝具值载体遗物的解析，外加遗物被替换时的状态迁移。
/// 载体可能是起始遗物「星之内海的圣剑使」，也可能是先古强化后的「不再遥远的希望之星」。
/// </summary>
internal static class NpRelic
{
    /// <summary>载体遗物被移走时快照下来的四个值。</summary>
    public readonly record struct Snapshot(int Np, int NpCardsPlayed, int SwitchCount, int GrowingBlade);

    private static readonly ConditionalWeakTable<Player, MyFirstCharacterRelic> _cache = new();

    /// <summary>等新遗物认领的迁移快照。用普通字典即可：Stash/Restore 成对，Restore 和 BeginCombat 都会清。</summary>
    private static readonly Dictionary<Player, Snapshot> _stash = new();

    public static MyFirstCharacterRelic? Carrier(Player? player)
    {
        if (player == null) return null;

        // HasBeenRemovedFromState 由 RemoveInternal() 置位，是 O(1) 的失效信号。
        // Owner 会调 AssertMutable，而移除动作不会翻转 IsMutable，所以这里安全。
        if (_cache.TryGetValue(player, out MyFirstCharacterRelic? cached)
            && !cached.HasBeenRemovedFromState
            && cached.Owner == player)
        {
            return cached;
        }

        MyFirstCharacterRelic? found = null;
        foreach (RelicModel relic in player.Relics)
        {
            if (relic is MyFirstCharacterRelic mine) { found = mine; break; }
        }

        if (found != null) _cache.AddOrUpdate(player, found);
        else _cache.Remove(player);
        return found;
    }

    public static MyFirstCharacterRelic? Carrier(Creature? creature)
        => creature == null ? null : Carrier(creature.Player);

    /// <summary>
    /// 遗物被移除时快照四个值，等替换进来的遗物认领。
    /// 先古事件「不再遥远的希望之星」走的是 RelicCmd.Replace = Remove(原) → Obtain(新, 原索引)。
    /// </summary>
    public static void Stash(MyFirstCharacterRelic relic)
    {
        Player? owner = relic.Owner;   // 规范实例上理论上可能是 null，实际不可达
        if (owner == null) return;
        _stash[owner] = new Snapshot(
            NpFields.Np[relic],
            NpFields.NpCardsPlayed[relic],
            NpFields.SwitchCount[relic],
            NpFields.GrowingBladeBonus[relic]);
    }

    /// <summary>
    /// 认领迁移快照，但**只填还是 Unset 的槽位** —— 别把刚由快照/存档导入的值盖掉。
    /// 没有快照时直接返回（开局 RunManager.FinalizeStartingRelics 也会走到这里，那时本来就是空的）。
    /// </summary>
    public static void Restore(MyFirstCharacterRelic relic)
    {
        Player? owner = relic.Owner;
        if (owner == null || !_stash.TryGetValue(owner, out Snapshot snapshot)) return;
        _stash.Remove(owner);

        if (NpFields.Np[relic] == NpFields.Unset) NpFields.Np[relic] = snapshot.Np;
        if (NpFields.NpCardsPlayed[relic] == NpFields.Unset) NpFields.NpCardsPlayed[relic] = snapshot.NpCardsPlayed;
        if (NpFields.SwitchCount[relic] == NpFields.Unset) NpFields.SwitchCount[relic] = snapshot.SwitchCount;
        if (NpFields.GrowingBladeBonus[relic] == NpFields.Unset) NpFields.GrowingBladeBonus[relic] = snapshot.GrowingBlade;
    }

    public static void ClearStash(Player player) => _stash.Remove(player);
}

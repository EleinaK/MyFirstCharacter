using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MyFirstCharacter.MyFirstCharacterCode.Relics;

namespace MyFirstCharacter.MyFirstCharacterCode.Patches;

/// <summary>
/// 多人同步的收尾补丁。宝具值等状态存在载体遗物上（见 FGOCore/NpFields），
/// 远端玩家快照落地后要把新值刷进 NpCharge 的兜底镜像。
/// </summary>
public static class NpSyncPatches
{
    /// <summary>
    /// Player.SyncWithSerializedPlayer 是远端快照唯一的回写点 —— 它内部走
    /// PopulateRelics → RelicModel.FromSerializable（BaseLib 的 ExtendedSavePatches 补过），
    /// 所以这个 postfix 跑的时候，遗物上的值已经是权威值了。
    ///
    /// 这里**只**刷镜像，两件事都不能做：
    ///   - 不调 NpCharge.SyncDisplay：同步永远发生在战斗外（CombatStateSynchronizer.StartSync 只在进房间前调用），
    ///     那时 Creature 上没有战斗 power，SyncDisplay 会走 Apply 分支，造出一个没有 CombatState 的野 power。
    ///     战斗开始时 BeforeCombatStartLate → BeginCombat → SyncDisplay 本来就会按导入的值重建显示条。
    ///   - 不触发 NpChanged：否则一次快照导入会被当成一次「获得」，重复往手牌补宝具卡。
    ///
    /// 另外注意：本地玩家不走这条路（WaitForSync 里 IsMe 的直接跳过），他是自己状态的权威，
    /// 只负责广播 —— 所以不需要在这里做任何主机/客户端的区分。
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SyncWithSerializedPlayer))]
    [HarmonyPostfix]
    public static void AfterSyncWithSerializedPlayer(Player __instance)
    {
        NpCharge.RefreshMirror(__instance);
    }

    /// <summary>
    /// 临时诊断：遗物从存档/网络包还原之后，四个字段到底有没有被填回来。
    /// 存档往返（Continue）和联机快照都走这个方法，所以它一行就能分开「没导入」和「导入了但没读到」。
    /// </summary>
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.FromSerializable))]
    [HarmonyPostfix]
    public static void AfterRelicFromSerializable(RelicModel __result)
    {
        if (__result is not MyFirstCharacterRelic relic) return;
        MainFile.Logger.Info($"[NP] 载入遗物 {relic.Id.Entry} np={NpFields.Np[relic]} "
            + $"卡数={NpFields.NpCardsPlayed[relic]} 切形={NpFields.SwitchCount[relic]} 成长={NpFields.GrowingBladeBonus[relic]}");
    }
}

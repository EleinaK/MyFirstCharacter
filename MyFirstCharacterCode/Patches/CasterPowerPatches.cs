using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MyFirstCharacter.MyFirstCharacterCode.Patches;

/// <summary>
/// Hardening patches: prevent crashes when card pool has no cards of certain rarities/types.
/// Our character only has Common and Basic cards so far, so we need fallbacks.
/// </summary>
public static class CasterPowerPatches
{
    /// <summary>If CreateForReward fails (e.g. no cards of requested rarity), retry with empty options.</summary>
    [HarmonyPatch(typeof(CardFactory), "CreateForReward", [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardCreationOptions)])]
    [HarmonyFinalizer]
    public static Exception? CreateForReward_Finalizer(Exception? __exception, Player player, CardCreationOptions options)
    {
        if (__exception is InvalidOperationException)
        {
            try
            {
                // Retry with empty options - game will pick from all available cards
                var method = AccessTools.Method(typeof(CardFactory), "CreateForReward",
                    [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardCreationOptions)]);
                method.Invoke(null, [player, Array.Empty<CardModel>(), options]);
                return null;
            }
            catch { return __exception; }
        }
        return __exception;
    }

    /// <summary>Merchant: fallback card type if no cards of requested type.</summary>
    [HarmonyPatch(typeof(CardFactory), "CreateForMerchant", [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType)])]
    [HarmonyPrefix]
    public static void CreateForMerchant_Type_Prefix(IEnumerable<CardModel> options, ref CardType type)
    {
        var list = options.Where(c => c.Rarity != CardRarity.Basic).ToList();
        if (list.Count == 0) return;
        var requestedType = type;
        if (!list.Any(c => c.Type == requestedType))
            type = list.GroupBy(c => c.Type).OrderByDescending(g => g.Count()).First().Key;
    }

    /// <summary>Merchant: fallback rarity if no cards of requested rarity.</summary>
    [HarmonyPatch(typeof(CardFactory), "CreateForMerchant", [typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardRarity)])]
    [HarmonyPrefix]
    public static bool CreateForMerchant_Rarity_Prefix(Player player, ref IEnumerable<CardModel> options, ref CardRarity rarity)
    {
        var valid = options.Where(c => c.Rarity != CardRarity.Basic).ToList();
        if (valid.Count == 0) return true;
        var requestedRarity = rarity;
        if (!valid.Any(c => c.Rarity == requestedRarity))
            rarity = valid.GroupBy(c => c.Rarity).OrderByDescending(g => g.Count()).First().Key;
        return true;
    }
}

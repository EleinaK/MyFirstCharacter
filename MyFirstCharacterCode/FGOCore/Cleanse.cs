using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MyFirstCharacter.MyFirstCharacterCode.FGOCore;

/// <summary>Ported from FGOCore: remove debuffs from creatures.</summary>
public static class Cleanse
{
    public static async Task<int> RemoveDebuffs(Creature target, int max = int.MaxValue, Func<PowerModel, bool>? keep = null)
    {
        var toRemove = target.Powers
            .Where(p => p.TypeForCurrentAmount == PowerType.Debuff && (keep == null || !keep(p)))
            .Take(max).ToList();
        foreach (var p in toRemove)
            await PowerCmd.Remove(p);
        return toRemove.Count;
    }
}

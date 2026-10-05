using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>
/// Anti-Purge Defense: negates HP loss for one hit, then decrements. Stackable (like Defect's Buffer).
/// </summary>
public class CasterInvinciblePower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>
    /// Reduces HP loss to 0. Late priority so other reductions trigger first.
    /// </summary>
    public override Decimal ModifyHpLostAfterOstyLate(Creature target, Decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return amount;
        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        await PowerCmd.Decrement(this);
    }
}

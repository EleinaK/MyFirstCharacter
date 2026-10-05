using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>
/// 圣剑使形态（形态一）：来自易伤状态的敌人的伤害降低为70%（类似巨像 ColossusPower）。
/// </summary>
public class CasterFormOnePower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldReceiveCombatHooks => true;

    public const string FramesPath = "res://MyFirstCharacter/character/artoria_frames_caster.tres";

    public override Decimal ModifyDamageMultiplicative(Creature target, Decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
    {
        if (target != Owner) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (dealer == null) return 1m;
        if (!dealer.HasPower<VulnerablePower>()) return 1m;
        return 0.7m;
    }
}

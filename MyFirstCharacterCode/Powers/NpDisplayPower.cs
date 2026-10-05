using MegaCrit.Sts2.Core.Entities.Powers;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>Visual-only NP counter power. Actual NP value stored in NpCharge dictionary.</summary>
public class NpDisplayPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

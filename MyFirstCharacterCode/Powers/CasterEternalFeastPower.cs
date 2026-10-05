using MegaCrit.Sts2.Core.Entities.Powers;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>不散之宴席：打出宝具卡时额外获得30点宝具值（结算在 CradlingYourHopeStar.OnPlay）。</summary>
public class CasterEternalFeastPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}

using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MyFirstCharacter.MyFirstCharacterCode.Cards;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>妖精骑士的试炼：本回合+2临时力量（回合结束回退）。</summary>
public class FairyKnightTrialStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<FairyKnightTrial>();
}

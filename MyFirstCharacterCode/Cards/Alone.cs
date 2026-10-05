using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

/// <summary>
/// 独自一人（无色牌）：保留、消耗。切换为另一种形态。升级后移除「消耗」。
/// </summary>
[Pool(typeof(CasterCardPool))]
public class Alone() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CasterCardHelpers.SwitchForm(ctx, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

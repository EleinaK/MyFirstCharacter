using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

/// <summary>
/// 灾厄（状态牌）：消耗。这张牌被消耗时，将此牌的复制品添加到你的弃牌堆。
/// </summary>
[Pool(typeof(CasterCardPool))]
public class CasterCalamity() : MyFirstCharacterCard(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { }

    /// <summary>被消耗（打出或其它效果）时，往弃牌堆加入一张复制品。</summary>
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this || CombatState == null || Owner == null) return;
        var copy = CombatState.CreateCard<CasterCalamity>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace MyFirstCharacter.MyFirstCharacterCode.FGOCore;

/// <summary>Ported from FGOCore: card manifesting (generating cards to hand).</summary>
public static class ManifestCards
{
    public const float DefaultPreviewTime = 1f;

    public static Task ManifestToHand<T>(Creature creature, float previewTime = 1f) where T : CardModel
    {
        return ManifestToPile<T>(creature, PileType.Hand, previewTime);
    }

    public static async Task ManifestToPile<T>(Creature creature, PileType pile, float previewTime = 1f) where T : CardModel
    {
        if (creature.CombatState != null && creature.Player != null)
        {
            T card = creature.CombatState.CreateCard<T>(creature.Player);
            await Manifest(creature, card, pile, previewTime);
        }
    }

    public static async Task Manifest(Creature creature, CardModel card, PileType pile, float previewTime = 1f)
    {
        if (creature.CombatState != null)
        {
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(card, pile, creature.Player), previewTime);
        }
    }
}

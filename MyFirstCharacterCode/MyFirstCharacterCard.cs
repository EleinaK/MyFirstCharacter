using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MyFirstCharacter.MyFirstCharacterCode.Extensions;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

public abstract class MyFirstCharacterCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target, true, true)
{
    // 每张卡用各自的小写蛇形名取图（如 ShiningLance -> shining_lance.png），找不到则回退到 card.png
    private string PortraitFile => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";

    public override string CustomPortraitPath => PortraitFile.BigCardImagePath();
    public override string PortraitPath => PortraitFile.CardImagePath();
    public override string BetaPortraitPath => PortraitFile.CardImagePath();

    public override List<(string, string)>? Localization => CasterLocalization.ForEntry(Id.Entry, "cards");
}

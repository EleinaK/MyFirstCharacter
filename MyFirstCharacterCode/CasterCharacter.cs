using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MyFirstCharacter.MyFirstCharacterCode.Cards;
using MyFirstCharacter.MyFirstCharacterCode.Extensions;
using MyFirstCharacter.MyFirstCharacterCode.Relics;

namespace MyFirstCharacter.MyFirstCharacterCode;

public class CasterCharacter : PlaceholderCharacterModel
{
    public const string CharacterId = "MyFirstCharacter";
    public static readonly Color Color = new("4a90d9");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;

    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<BlueCard>(), ModelDb.Card<BlueCard>(),
        ModelDb.Card<CasterStrike>(), ModelDb.Card<CasterStrike>(),
        ModelDb.Card<DefendCaster>(), ModelDb.Card<DefendCaster>(), ModelDb.Card<DefendCaster>(), ModelDb.Card<DefendCaster>(),
        ModelDb.Card<LakeProtectionA>(),
        ModelDb.Card<SummerBeach>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics => [
        ModelDb.Relic<CasterStartingRelic>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<CasterCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<CasterRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<CasterPotionPool>();

    // === Custom visual scenes ===

    public override string CustomVisualPath => "res://MyFirstCharacter/scenes/artoria_visuals.tscn";

    protected override IEnumerable<string> ExtraAssetPaths => [
        "res://MyFirstCharacter/character/artoria_frames_caster.tres",
        "res://MyFirstCharacter/character/artoria_frames_avalon.tres",
    ];

    // Custom character select background — temporarily disabled
    public override string CustomCharacterSelectBg => "res://MyFirstCharacter/scenes/artoria_select_bg.tscn";

    public override string CustomMerchantAnimPath => "res://MyFirstCharacter/scenes/artoria_merchant.tscn";

    public override string CustomRestSiteAnimPath => "res://MyFirstCharacter/scenes/artoria_rest.tscn";

    public override NCreatureVisuals? CreateCustomVisuals()
    {
        if (!string.IsNullOrEmpty(CustomVisualPath))
            return NodeFactory<NCreatureVisuals>.CreateFromScene(CustomVisualPath);
        return null;
    }

    // === Character select UI ===

    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();

    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();

    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();

    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();

    public override Control CustomIcon
    {
        get
        {
            Control control = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
            return control;
        }
    }
}

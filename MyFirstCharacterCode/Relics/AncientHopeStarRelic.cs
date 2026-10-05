using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MyFirstCharacter.MyFirstCharacterCode.Cards;
using MyFirstCharacter.MyFirstCharacterCode.Extensions;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MyFirstCharacter.MyFirstCharacterCode.Powers;

namespace MyFirstCharacter.MyFirstCharacterCode.Relics;

/// <summary>先古遗物「不再遥远的希望之星」：替换起始遗物，形态入场效果强化（+30NP / +4力量）。</summary>
[Pool(typeof(CasterRelicPool))]
public class AncientHopeStarRelic : MyFirstCharacterRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;

    // 遗物图标与起始遗物一致
    public override string PackedIconPath => "casterstartingrelic.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "casterstartingrelic_outline.png".RelicImagePath();
    protected override string BigIconPath => "casterstartingrelic.png".BigRelicImagePath();

    public override async Task BeforeCombatStartLate()
    {
        await base.BeforeCombatStartLate();
        var c = Owner.Creature;
        NpCharge.ResetCombat(c);
        if (!c.HasPower<CasterFormOnePower>() && !c.HasPower<CasterFormTwoPower>())
        {
            await PowerCmd.Apply<CasterFormTwoPower>(new BlockingPlayerChoiceContext(), c, 1, c, null, false);
            SpriteController.ApplyFrames(c, CasterFormTwoPower.FramesPath);
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), c, 4, c, null, false);
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), c, 1, c, null, false);
            await NpCharge.SyncDisplay(c);
            await CasterCardHelpers.CheckNPThreshold(c);
        }
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner == Owner)
            TurnTracker.MarkExhausted(Owner.Creature);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            TurnTracker.ClearExhausted(Owner.Creature);
    }
}

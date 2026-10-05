using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MyFirstCharacter.MyFirstCharacterCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

// ===== 普通卡（Phase 2 新增） =====

[Pool(typeof(CasterCardPool))] public class FairyKnightTrial() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Move), new RepeatVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await PowerCmd.Apply<FairyKnightTrialStrengthPower>(ctx, Owner.Creature, 2, Owner.Creature, this, false);
        for (int i = 0; i < DynamicVars.Repeat.BaseValue; i++)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Repeat.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class GaleBlade() : MyFirstCharacterCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        var copy = CombatState.CreateCard<GaleBlade>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(copy);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Top);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); }
}

[Pool(typeof(CasterCardPool))] public class StarCharge() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Common, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 10m), new DynamicVar("NextNp", 20m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await NpCharge.Gain(Owner.Creature, IsUpgraded ? 15 : 10);
        await PowerCmd.Apply<CasterNextTurnNpPower>(ctx, Owner.Creature, IsUpgraded ? 25 : 20, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); DynamicVars["NextNp"].UpgradeValueBy(5m); }
}

[Pool(typeof(CasterCardPool))] public class StarGuidance() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Common, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 10m), new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await NpCharge.Gain(Owner.Creature, IsUpgraded ? 15 : 10);
        await CardPileCmd.Draw(ctx, IsUpgraded ? 2 : 1, Owner);
    }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); DynamicVars.Cards.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class FairyLullaby() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Common, TargetType.Self) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int count = IsUpgraded ? 2 : 1;
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var chosen = await CardSelectCmd.FromCombatPile(ctx, exhaustPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count));
        foreach (var card in chosen)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class RepayAll() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Common, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await PlayerCmd.GainEnergy(2, Owner);
        await PowerCmd.Apply<CasterRepayAllPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MyFirstCharacter.MyFirstCharacterCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

// ===== 稀有卡（Phase 4） =====

// ---- 攻击 ----

[Pool(typeof(CasterCardPool))] public class ShiningLance() : MyFirstCharacterCard(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(65m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, -2, Owner.Creature, this, false);
        await PowerCmd.Apply<WeakPower>(ctx, Owner.Creature, 2, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(15m); }
}

[Pool(typeof(CasterCardPool))] public class BladeDraw() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var attacks = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type == CardType.Attack && c != (CardModel)this).ToList();
        int total = 0;
        foreach (var a in attacks) {
            try { total += (int)a.DynamicVars.Damage.BaseValue; } catch { }
            await CardCmd.Exhaust(ctx, a);
        }
        if (total > 0)
            await PowerCmd.Apply<BladeDrawStrengthPower>(ctx, Owner.Creature, total, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class ForgiveOurSins() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var hand = PileType.Hand.GetPile(Owner).Cards.Where(c => c != (CardModel)this).ToList();
        int count = hand.Count;
        foreach (var c in hand) await CardCmd.Exhaust(ctx, c);
        // 读声明的变量而不是硬编码 7/5：写死的话 DynamicVars.Damage 从来没有升级过，
        // 卡面的 {Damage:diff()} 两个状态都渲染成 5 —— 升级后实打 7 点、卡面却仍显示 5。
        int dmg = (int)DynamicVars.Damage.BaseValue;
        for (int i = 0; i < count; i++)
            await DamageCmd.Attack((decimal)dmg).FromCard(this).TargetingAllOpponents(CombatState).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); }
}

[Pool(typeof(CasterCardPool))] public class OrkneyRain() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move), new DynamicVar("Mult", 1m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int dmg = (int)DynamicVars.Damage.BaseValue;
        int mult = IsUpgraded ? 4 : 2;
        for (int i = 0; i < NpCharge.NpCardsPlayed(Owner.Creature); i++) dmg *= mult;
        await DamageCmd.Attack((decimal)dmg).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); DynamicVars["Mult"].UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class RingSixBells() : MyFirstCharacterCard(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new RepeatVar(6)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        for (int i = 0; i < DynamicVars.Repeat.BaseValue; i++)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Repeat.UpgradeValueBy(2m); }
}

/// <summary>妖精圆桌领域：本局伤害永久成长（按玩家持久，跨战斗继承，随新一局重置）。</summary>
public static class GrowingBladeTracker
{
    private static readonly Dictionary<Player, int> _bonus = new();
    public static int Bonus(Player player) => _bonus.GetValueOrDefault(player, 0);
    public static void Increment(Player player, int amount) => _bonus[player] = Bonus(player) + amount;
}

[Pool(typeof(CasterCardPool))] public class GrowingBlade() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move), new DynamicVar("Growth", 3m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + GrowingBladeTracker.Bonus(Owner)).FromCard(this).Targeting(play.Target).Execute(ctx);
        GrowingBladeTracker.Increment(Owner, IsUpgraded ? 5 : 3);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["Growth"].UpgradeValueBy(2m); }
}

[Pool(typeof(CasterCardPool))] public class SalvationSword() : MyFirstCharacterCard(10, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(45m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(15m); }
}

// ---- 技能 ----

[Pool(typeof(CasterCardPool))] public class FurnaceFairy() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(5)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int keep = 5;
        var drawPile = PileType.Draw.GetPile(Owner);
        var all = drawPile.Cards.ToList();
        if (all.Count == 0) return;
        var keepCards = await CardSelectCmd.FromCombatPile(ctx, drawPile, Owner, IsUpgraded
            ? new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, keep)
            : new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, keep));
        var keepSet = keepCards.ToHashSet();
        var unselected = all.Where(c => !keepSet.Contains(c)).ToList();
        int half = unselected.Count / 2;
        var rng = Owner.PlayerRng.Rewards;
        foreach (var c in unselected.OrderBy(c => rng.NextInt(int.MaxValue)).Take(half).ToList())
            await CardCmd.Exhaust(ctx, c);
    }
    protected override void OnUpgrade() { }
}

[Pool(typeof(CasterCardPool))] public class CalamityIsEvil() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    /// <summary>把衍生卡「CasterCalamity」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<CasterCalamity>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("AddCalamity", 2m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        if (CombatState == null) return;          // 后续要 CreateCard，先挡掉空引用
        int repeats = IsUpgraded ? 2 : 1;
        int add = (int)DynamicVars["AddCalamity"].BaseValue;
        for (int i = 0; i < repeats; i++) {
            for (int k = 0; k < add; k++)
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                    CombatState.CreateCard<CasterCalamity>(Owner), PileType.Hand, Owner));
            var calamities = new List<CardModel>();
            foreach (var pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
                calamities.AddRange(pile.GetPile(Owner).Cards.Where(c => c is CasterCalamity).ToList());
            foreach (var c in calamities) await CardCmd.Exhaust(ctx, c);
        }
    }
    // 升级只把描述里的「那之后，再执行一次。」加上；执行次数由 IsUpgraded 决定（1 -> 2），无需 DynamicVar
    protected override void OnUpgrade() { }
}

[Pool(typeof(CasterCardPool))] public class EntrustedFuture() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int count = IsUpgraded ? 3 : 2;
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var chosen = await CardSelectCmd.FromCombatPile(ctx, exhaustPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count));
        foreach (var card in chosen)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class UnreceivedHairpin() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int x = ResolveEnergyXValue();
        int times = x * DynamicVars.Repeat.IntValue;   // 倍数 = 1 / 升级 2，读的与卡面同一份声明
        for (int i = 0; i < times; i++)
            await CasterCardHelpers.SwitchForm(ctx, this);
    }
    protected override void OnUpgrade() { DynamicVars.Repeat.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class GiveStars() : MyFirstCharacterCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var toMove = new List<CardModel>();
        toMove.AddRange(PileType.Hand.GetPile(Owner).Cards.Where(c => c != (CardModel)this));
        toMove.AddRange(PileType.Discard.GetPile(Owner).Cards);
        foreach (var c in toMove)
            await CardPileCmd.Add(c, PileType.Draw, CardPilePosition.Random);
        await CardPileCmd.Shuffle(ctx, Owner);
        int exhaustCount = PileType.Exhaust.GetPile(Owner).Cards.Count;
        await CardPileCmd.Draw(ctx, exhaustCount, Owner);
        if (IsUpgraded)
            await PowerCmd.Apply<CasterInvinciblePower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { }
}

[Pool(typeof(CasterCardPool))] public class CalamityTorrent() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    /// <summary>把衍生卡「CasterCalamity」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<CasterCalamity>(), HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    // 卡片自带「消耗」：文案与关键字都要有（实现里原先漏了关键字）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 50m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await NpCharge.Gain(Owner.Creature, (int)DynamicVars["Np"].BaseValue);   // 读声明，别再写一份字面量
        int current = PileType.Hand.GetPile(Owner).Cards.Count;
        int toAdd = Math.Max(0, 10 - current);
        for (int i = 0; i < toAdd; i++) {
            var cal = CombatState.CreateCard<CasterCalamity>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(cal, PileType.Hand, Owner));
        }
    }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(20m); }   // 50 -> 70
}

[Pool(typeof(CasterCardPool))] public class DistantHope() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var chosen = await CardSelectCmd.FromCombatPile(ctx, exhaustPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1));
        foreach (var card in chosen) {
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
            card.EnergyCost.SetThisTurnOrUntilPlayed(Math.Max(0, card.EnergyCost.GetResolved() - 1));
        }
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class DeparturePoem() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var chosen = await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), c => c != (CardModel)this, this);
        var target = chosen.FirstOrDefault();
        if (target == null) return;
        // 必须用 CardModel.CreateClone()：它会走 CardScope 建副本、登记 _cloneOf 并清掉
        // ExhaustOnNextPlay。原来的 ClonePreservingMutability() 在规范实例上直接返回原牌本身
        // （于是 AddGeneratedCardToCombat 会抛「不能生成已属于某个牌堆的牌」），在可变实例上
        // 也只做浅拷贝、不登记 clone 关系。游戏自己的复制效果（DualWield）用的就是 CreateClone()。
        var copy = target.CreateClone();
        if (IsUpgraded)
            await CardPileCmd.Add(target, PileType.Draw, CardPilePosition.Top);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Top);
    }
    protected override void OnUpgrade() { }
}

// ---- 能力 ----

[Pool(typeof(CasterCardPool))] public class GiftedSpringMemory() : MyFirstCharacterCard(3, CardType.Power, CardRarity.Rare, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<CasterDoubleNpPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { RemoveKeyword(CardKeyword.Ethereal); }
}

[Pool(typeof(CasterCardPool))] public class ParadiseFairy() : MyFirstCharacterCard(3, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<ParadiseFairyPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class FairyForm() : MyFirstCharacterCard(3, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<StrengthPower>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<FairyFormPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class Pilgrimage() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<PilgrimagePower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class OnceSeenFinalStar() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Rare, TargetType.Self) {
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CardPileCmd.Draw(ctx, 2, Owner);
        await PowerCmd.Apply<OnceSeenFinalStarPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }
}

[Pool(typeof(CasterCardPool))] public class BodyIsSword() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 5m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<BodyIsSwordPower>(ctx, Owner.Creature, IsUpgraded ? 10 : 5, Owner.Creature, this, false); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); }
}

[Pool(typeof(CasterCardPool))] public class AloneSeeksStars() : MyFirstCharacterCard(3, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>把衍生卡「Alone」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Alone>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<AloneSeeksStarsPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class StarBirthMoment() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block), HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<CasterDeathRiskPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); }
}

[Pool(typeof(CasterCardPool))] public class GloucesterDay11() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<GloucesterDay11Power>(ctx, Owner.Creature, IsUpgraded ? 6 : 4, Owner.Creature, this, false); }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); }
}

[Pool(typeof(CasterCardPool))] public class FairyEnd() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<FairyEndPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }
}

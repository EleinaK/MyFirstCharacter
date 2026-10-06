using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MyFirstCharacter.MyFirstCharacterCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

// ===== 罕见卡（Phase 3） =====

// ---- 攻击 ----

[Pool(typeof(CasterCardPool))] public class MirrorClanDestruction() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(11m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); }
}

[Pool(typeof(CasterCardPool))] public class StarFurnace() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move), new DynamicVar("NpThreshold", 50m), new DynamicVar("Enhanced", 14m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int threshold = IsUpgraded ? 40 : 50;
        int dmg = (int)DynamicVars.Damage.BaseValue;
        if (NpCharge.Current(Owner.Creature) >= threshold) {
            await DamageCmd.Attack((decimal)(dmg + 5)).FromCard(this).Targeting(play.Target).Execute(ctx);
            await CardCmd.Exhaust(ctx, this);
        } else {
            await DamageCmd.Attack((decimal)dmg).FromCard(this).Targeting(play.Target).Execute(ctx);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); DynamicVars["NpThreshold"].UpgradeValueBy(-10m); DynamicVars["Enhanced"].UpgradeValueBy(3m); }
}

[Pool(typeof(CasterCardPool))] public class AbsoluteLoneliness() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>把衍生卡「Alone」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Alone>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        foreach (var pile in new[] { PileType.Draw, PileType.Hand, PileType.Discard }) {
            var alone = CombatState.CreateCard<Alone>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(alone, pile, Owner));
        }
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); }
}

[Pool(typeof(CasterCardPool))] public class HolySwordFlash() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target)
            .WithHitVfxNode(NBigSlashVfx.Create)
            .WithHitVfxNode(NBigSlashImpactVfx.Create)
            .Execute(ctx);
        if (play.Target.HasPower<VulnerablePower>())
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target)
                .WithHitVfxNode(NBigSlashVfx.Create)
                .WithHitVfxNode(NBigSlashImpactVfx.Create)
                .Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); }
}

[Pool(typeof(CasterCardPool))] public class ChosenSword() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target)
            .WithHitVfxNode(NBigSlashVfx.Create)
            .WithHitVfxNode(NBigSlashImpactVfx.Create)
            .Execute(ctx);
        var top = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault();
        if (top != null) await CardCmd.Exhaust(ctx, top);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); }
}

[Pool(typeof(CasterCardPool))] public class ProphecyLight() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move), new DynamicVar("Look", 3m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        int look = IsUpgraded ? 5 : 3;
        var top = PileType.Draw.GetPile(Owner).Cards.Take(look).ToList();
        if (top.Count == 0) return;
        var chosen = await CardSelectCmd.FromSimpleGrid(ctx, top, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1));
        foreach (var card in chosen)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["Look"].UpgradeValueBy(2m); }
}

[Pool(typeof(CasterCardPool))] public class HolySwordJudgment() : MyFirstCharacterCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(27m, ValueProp.Move), new DynamicVar("Np", 40m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var cmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        bool killed = false;
        foreach (var list in cmd.Results)
            foreach (var r in list)
                if (r.WasTargetKilled) { killed = true; break; }
        if (killed)
            await NpCharge.Gain(Owner.Creature, IsUpgraded ? 50 : 40);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(8m); DynamicVars["Np"].UpgradeValueBy(10m); }
}

[Pool(typeof(CasterCardPool))] public class CalamityCombo() : MyFirstCharacterCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>把衍生卡「CasterCalamity」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<CasterCalamity>()];

    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int x = ResolveEnergyXValue();
        int times = x + (IsUpgraded ? 1 : 0);
        for (int i = 0; i < times; i++)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        for (int i = 0; i < x; i++) {
            var cal = CombatState.CreateCard<CasterCalamity>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(cal, PileType.Discard, Owner));
        }
    }
    protected override void OnUpgrade() { }
}

[Pool(typeof(CasterCardPool))] public class StrikeWeakPoint() : MyFirstCharacterCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];

    private const string VulnVarName = "VulnMult";

    /// <summary>
    /// 「敌人身上每层易伤 +N 点」这部分加成是在 OnPlay 里手工并进 DamageCmd 的，不走伤害预览
    /// 管线 —— 所以卡面永远只显示基础伤害（3/5），和实际会打出的 (3 + 层数×2) 对不上。
    /// 这个自定义 DamageVar 在预览时把同一份加成算进去。
    ///
    /// 做法：临时把 BaseValue 代入加成值，再调用引擎自己的 base.UpdateCardPreview，于是加成会
    /// 和实算一样**先过全局修正**（易伤本身的 ×1.5、力量等），算完立刻还原字段。
    /// 只在 base 之后再加是错的：那样得到 BaseValue×修正 + 层数×per，而实际是
    /// (BaseValue + 层数×per)×修正，正好差 层数×per×(修正-1)。
    ///
    /// 不直接抄一份 base 的实现，是因为那要复制 Hook.ModifyDamage / EnchantmentModel 等引擎
    /// 内部调用，游戏更新后容易悄悄失准；委托给它反而稳。
    ///（副作用：PreviewValue 会高于 EnchantedValue，于是 {Damage:diff()} 会把数字显示成
    ///  「被提升」的高亮态 —— 对着带易伤的敌人时数字变绿。算是有用的提示，不要的话说一声。）
    /// </summary>
    private sealed class VulnerableScaledDamageVar(decimal damage) : DamageVar(damage, ValueProp.Move) {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks) {
            int stacks = target?.GetPower<VulnerablePower>() is { } v ? (int)v.Amount : 0;
            if (stacks == 0) { base.UpdateCardPreview(card, previewMode, target, runGlobalHooks); return; }

            decimal per = card.DynamicVars[VulnVarName].BaseValue;
            decimal saved = BaseValue;
            decimal preview;
            try {
                BaseValue = saved + stacks * per;
                base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
                preview = PreviewValue;
            }
            finally {
                BaseValue = saved;   // BaseValue 的 setter 会 ResetToBase()，把 PreviewValue 冲掉
            }
            PreviewValue = preview;  // 所以算好的显示值要在这里写回
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new VulnerableScaledDamageVar(3m), new DynamicVar(VulnVarName, 2m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var vulnPower = play.Target.GetPower<VulnerablePower>();
        int vuln = vulnPower != null ? (int)vulnPower.Amount : 0;
        // 读声明的变量而不是硬编码 2/3，免得以后改升级值忘了改这里
        int per = (int)DynamicVars[VulnVarName].BaseValue;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + vuln * per).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars[VulnVarName].UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class AttackDefenseOne() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move), new BlockVar(10m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); DynamicVars.Block.UpgradeValueBy(3m); }
}

[Pool(typeof(CasterCardPool))] public class HuntMark() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move), new DynamicVar("Np", 10m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        await PowerCmd.Apply<HuntMarkPower>(ctx, play.Target, IsUpgraded ? 15 : 10, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); }
}

// ---- 技能 ----

[Pool(typeof(CasterCardPool))] public class MirrorClanSecretRite() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var drawPile = PileType.Draw.GetPile(Owner);
        var chosen = await CardSelectCmd.FromCombatPile(ctx, drawPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, 3));
        foreach (var card in chosen)
            await CardCmd.Exhaust(ctx, card);
    }
    protected override void OnUpgrade() { RemoveKeyword(CardKeyword.Exhaust); }
}

[Pool(typeof(CasterCardPool))] public class BookBurningRite() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int count = IsUpgraded ? 2 : 1;
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var toHand = await CardSelectCmd.FromCombatPile(ctx, exhaustPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count));
        foreach (var card in toHand)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
        var drawPile = PileType.Draw.GetPile(Owner);
        var toExhaust = await CardSelectCmd.FromCombatPile(ctx, drawPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, count));
        foreach (var card in toExhaust)
            await CardCmd.Exhaust(ctx, card);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class BloodCrown() : MyFirstCharacterCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>把衍生卡「Alone」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Alone>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var hand = PileType.Hand.GetPile(Owner).Cards.Where(c => c != (CardModel)this).ToList();
        if (hand.Count == 0) return;
        var chosen = await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, hand.Count), c => c != (CardModel)this, this);
        foreach (var card in chosen) {
            var alone = CombatState.CreateCard<Alone>(Owner);
            await CardCmd.Transform(card, alone, CardPreviewStyle.None);
        }
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class UndegradedFairyEye() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move), new PowerVar<WeakPower>(1m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        int weak = IsUpgraded ? 2 : 1;
        // CombatState.Enemies 是**未过滤存活**的整个敌方列表；不过滤会往已经死掉的敌人身上挂虚弱。
        // 同类的星之悸动、威尔士森林都写了 .Where(e => e.IsAlive)。
        foreach (var e in CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<WeakPower>(ctx, e, weak, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); DynamicVars.Weak.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class Reclaim() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int count = IsUpgraded ? 3 : 2;
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var chosen = await CardSelectCmd.FromCombatPile(ctx, exhaustPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count));
        foreach (var card in chosen)
            await CardPileCmd.Add(card, PileType.Discard, CardPilePosition.Bottom);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class RainstormComing() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>把衍生卡「CasterCalamity」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<CasterCalamity>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var discardPile = PileType.Discard.GetPile(Owner);
        var chosen = await CardSelectCmd.FromCombatPile(ctx, discardPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2));
        foreach (var card in chosen)
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
        var cal = CombatState.CreateCard<CasterCalamity>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(cal, PileType.Discard, Owner));
    }
    protected override void OnUpgrade() { RemoveKeyword(CardKeyword.Exhaust); }
}

[Pool(typeof(CasterCardPool))] public class FairyDance() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockGain", 5m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await PowerCmd.Apply<FairyDancePower>(ctx, Owner.Creature, IsUpgraded ? 8 : 5, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars["BlockGain"].UpgradeValueBy(3m); }
}

[Pool(typeof(CasterCardPool))] public class MirrorCopy() : MyFirstCharacterCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(14m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        var hand = PileType.Hand.GetPile(Owner).Cards.Where(c => c != (CardModel)this).ToList();
        if (hand.Count < 2) return;
        // 一次选 2 张，按点击顺序：第 1 张 = 被变化的，第 2 张 = 复制来源。
        //
        // 原来这里连着调了两次 CardSelectCmd.FromHand。全 mod 只有这张卡这么做 ——
        // 原生 GUARDS 和我们的「吾乃撕裂星辰之雷霆」都只选一次，实测也只有这张卡会出现
        // 两张透明卡（节点被挂错父节点、回合结束清手牌时 Godot 报 already has a parent）。
        // 先把两次选择合并成一次，消掉这个差异。
        var chosen = (await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2, 2), c => c != (CardModel)this, this)).ToList();
        if (chosen.Count < 2) return;
        var target = chosen[0];
        var source = chosen[1];

        // 用 CombatState.CreateCard(规范实例, Owner) **新建**一张替换体。
        // 游戏原生所有变形效果（Guards / Compact ...）都是这个写法，也是唯一能让手牌正确
        // 刷新的路径（走 canonical.ToMutable() + AddCard + AfterCreated）。
        //
        // 原来这里用的是 source.CreateClone()，症状是**被选中的两张牌都变成空白卡**。
        // CreateClone() 的定位是「往牌堆里塞一张复制品」（见 DeparturePoem / FairyAfterimage，
        // 它们配 AddGeneratedCardToCombat 用，那是对的），拿它当变形替换体会让手牌渲染不出来。
        // 也不能把 source 本身传进去：Transform 会把它从手牌移走，而它还在玩家刚选完的列表里。
        var combat = CombatState;
        if (combat == null) return;
        CardModel replacement = combat.CreateCard(source.CanonicalInstance, Owner);
        if (source.IsUpgraded) CardCmd.Upgrade(replacement);
        await CardCmd.Transform(target, replacement);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(4m); }
}

[Pool(typeof(CasterCardPool))] public class ProphecyChildJourney() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new DynamicVar("CardsExtra", 4m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        bool exhausted = TurnTracker.HasExhausted(Owner.Creature);
        int draw = exhausted ? (IsUpgraded ? 5 : 4) : (IsUpgraded ? 3 : 2);
        await CardPileCmd.Draw(ctx, draw, Owner);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); DynamicVars["CardsExtra"].UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class FairyAfterimage() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        var copy = CombatState.CreateCard<FairyAfterimage>(Owner);
        if (IsUpgraded) CardCmd.Upgrade(copy);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); }
}

// ---- 能力 ----

[Pool(typeof(CasterCardPool))] public class StarBarrier() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block), HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<StarBarrierPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }
}

[Pool(typeof(CasterCardPool))] public class FlowerGarden() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<FlowerGardenPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); }
}

[Pool(typeof(CasterCardPool))] public class MirrorClanCovenant() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<MirrorClanCovenantPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); }
}

[Pool(typeof(CasterCardPool))] public class CalamityConversion() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 5m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<CalamityConversionPower>(ctx, Owner.Creature, (int)DynamicVars["Np"].BaseValue, Owner.Creature, this, false); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); }
}

[Pool(typeof(CasterCardPool))] public class ExhaustRefinement() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 5m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<ExhaustRefinementPower>(ctx, Owner.Creature, IsUpgraded ? 10 : 5, Owner.Creature, this, false); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); }
}

[Pool(typeof(CasterCardPool))] public class FormFlow() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<FormFlowPower>(ctx, Owner.Creature, DynamicVars.Cards.IntValue, Owner.Creature, this, false); }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

[Pool(typeof(CasterCardPool))] public class KeptStarThrob() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("StrengthLoss", 9m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int amount = IsUpgraded ? 13 : 9;
        foreach (var e in CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<KeptStarThrobPower>(ctx, e, amount, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars["StrengthLoss"].UpgradeValueBy(4m); }
}

[Pool(typeof(CasterCardPool))] public class StarRendingThunder() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Dazed>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<StarRendingThunderPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }
}

[Pool(typeof(CasterCardPool))] public class LakeFairy() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<FrailPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<LakeFairyPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

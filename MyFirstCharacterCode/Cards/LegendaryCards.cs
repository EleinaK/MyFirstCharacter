using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MyFirstCharacter.MyFirstCharacterCode.Powers;
using MyFirstCharacter.MyFirstCharacterCode.Relics;
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
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var attacks = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.Type == CardType.Attack && c != (CardModel)this).ToList();

        // 只做**一遍**判定：按原生「痛殴」(Thrash) 的三选一级联取牌面伤害，再过一次 Hook.ModifyDamage
        // （dealer 传自己 → 吃玩家当前力量；target 传 null → 敌人易伤不计入，与 Thrash 完全一致）。
        // 这个结果同时就是本回合施加的临时力量**和**宝具值本身。
        // 顺序必须是「先算完 → 再施加力量 → 最后给宝具值」：先算后加，本卡刚给的力量不会回流进自己的 total。
        int total = 0;
        foreach (var a in attacks) {
            decimal damage;
            if (a.DynamicVars.ContainsKey("CalculatedDamage")) damage = a.DynamicVars.CalculatedDamage.Calculate(null);
            else if (a.DynamicVars.ContainsKey("Damage"))      damage = a.DynamicVars.Damage.BaseValue;
            else if (a.DynamicVars.ContainsKey("OstyDamage"))  damage = a.DynamicVars.OstyDamage.BaseValue;
            else { damage = 0m; Log.Warn(Id.Entry + " 手牌攻击牌 " + a.Id.Entry + " 没有可用的伤害变量，按 0 计"); }
            damage = Hook.ModifyDamage(Owner.RunState, Owner.Creature.CombatState, null, Owner.Creature, damage,
                                       ValueProp.Move, a, ModifyDamageHookType.All, CardPreviewMode.None, out IEnumerable<AbstractModel> _);
            total += (int)damage;
        }

        if (total > 0)
            await PowerCmd.Apply<BladeDrawStrengthPower>(ctx, Owner.Creature, total, Owner.Creature, this, false);
        foreach (var a in attacks)
            await CardCmd.Exhaust(ctx, a);
        // 宝具值 = **施加临时力量之后**身上的力量总值（原有的 + 本卡给的），升级后 ×3。
        // 不是「本卡给的那一份」—— 例如原有力量 2、本卡给 96，则应获得 98。
        int strAfter = Owner.Creature.GetPowerAmount<StrengthPower>();
        await NpCharge.Gain(Owner.Creature, strAfter * (IsUpgraded ? 3 : 1));
    }
    // 升级不再减费（费用保持 2），只提高宝具值倍率 —— 倍率写在 OnPlay 里，卡面用 {IfUpgraded} 表达
    protected override void OnUpgrade() { }
}

[Pool(typeof(CasterCardPool))] public class ForgiveOurSins() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];
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

[Pool(typeof(CasterCardPool))] public class RingSixBells() : MyFirstCharacterCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new RepeatVar(6)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        // 「失去当前所有宝具值」= 归零。Spend 的判定是 cur < amount 才失败，传 amount == cur 必然通过。
        // 归零后 NpChanged → CheckNPThreshold 因 IsOvercharged(0)=false 不会补宝具卡。
        await NpCharge.Spend(Owner.Creature, NpCharge.Current(Owner.Creature));
        for (int i = 0; i < DynamicVars.Repeat.BaseValue; i++)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(1m); DynamicVars.Repeat.UpgradeValueBy(1m); }
}

/// <summary>妖精圆桌领域：本局伤害永久成长（按玩家持久，跨战斗继承，随新一局重置）。
/// 真实存储挂在载体遗物上（见 FGOCore/NpFields），所以它也会随存档和多人权威快照一起走；
/// 玩家身上找不到载体遗物时退化成本地字典。</summary>
public static class GrowingBladeTracker
{
    private static readonly Dictionary<Player, int> _fb = new();

    public static int Bonus(Player player)
    {
        MyFirstCharacterRelic? relic = NpRelic.Carrier(player);
        if (relic != null)
        {
            int fromRelic = NpFields.GrowingBladeBonus[relic];
            if (fromRelic != NpFields.Unset) return fromRelic;
        }
        return _fb.GetValueOrDefault(player, 0);
    }

    public static void Increment(Player player, int amount)
    {
        int next = Bonus(player) + amount;
        _fb[player] = next;
        MyFirstCharacterRelic? relic = NpRelic.Carrier(player);
        if (relic != null) NpFields.GrowingBladeBonus[relic] = next;
    }
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

    /// <summary>手牌 / 抽牌堆 / 弃牌堆 / **消耗牌堆** 四处「灾厄」的总数（与卡面文案一致）。
    /// 消耗牌堆要算：被消耗的灾厄本体进消耗牌堆、复制品进弃牌堆，设计上两处都计入格挡。</summary>
    private static decimal CountCalamity(CardModel card) {
        Player? owner = card.Owner;
        if (owner == null) return 0m;      // 图鉴/规范实例上 Owner 可能为空
        int n = 0;
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
            n += pileType.GetPile(owner).Cards.Count(c => c is CasterCalamity);
        return n;
    }

    // 走原生 Stack 同款的 CalculatedBlockVar：CustomCardModel.GainsBlock 按「DynamicVars 里有没有
    // BlockVar/CalculatedBlockVar」自动推导，于是卡框和「格挡」悬停提示都自动来。
    // CalculatedVar.Calculate() = CalculationBase + CalculationExtra × multiplier()，
    // 所以 base=0 + extra=1 时算出来就是 multiplier 本身。
    // ⚠ CalculationBase/CalculationExtra **两个都必须声明**：SetOwner→UpdateValues 与 Calculate 都会查
    // 这两个键的索引器，缺了直接抛 KeyNotFoundException —— 而 SetOwner 在卡牌网格构建期就会跑，
    // 会让抽牌堆/卡组界面整个空白（本仓库出过这个事故）。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("AddCalamity", 2m),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => CountCalamity(card)),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        if (CombatState == null) return;          // 后续要 CreateCard，先挡掉空引用
        int repeats = IsUpgraded ? 2 : 1;
        int add = (int)DynamicVars["AddCalamity"].BaseValue;
        for (int i = 0; i < repeats; i++) {
            // 1) 向手牌添加 2 张「灾厄」
            for (int k = 0; k < add; k++)
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                    CombatState.CreateCard<CasterCalamity>(Owner), PileType.Hand, Owner));
            // 2) 消耗所有「灾厄」（手牌 / 抽牌堆 / 弃牌堆）
            var calamities = new List<CardModel>();
            foreach (var pile in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
                calamities.AddRange(pile.GetPile(Owner).Cards.Where(c => c is CasterCalamity).ToList());
            foreach (var c in calamities) await CardCmd.Exhaust(ctx, c);
            // 3) 再向弃牌堆添加「等量」灾厄 —— 数量 = 本次消耗掉的张数。
            //    ⚠ 这一步不能省：CasterCalamity 自身的 AfterCardExhausted 只会给每张被消耗的灾厄补 1 张复制品
            //    （那是状态牌自己的被动），卡牌文案里的「并添加等量「灾厄」到你的弃牌堆」是额外的一份。
            //    旧实现漏了这一步 → 例如当前无灾厄时打出：消耗 2 张，弃牌堆只有 2 张（应为 4 张）。
            for (int k = 0; k < calamities.Count; k++)
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                    CombatState.CreateCard<CasterCalamity>(Owner), PileType.Discard, Owner));
        }
        // 「那之后」= 全部重复结算完之后再数一次（手牌 / 抽牌堆 / 弃牌堆 / 消耗牌堆 四处）
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(play.Target),
                                    DynamicVars.CalculatedBlock.Props, play);
    }
    // 升级只多执行一次（重复次数由 IsUpgraded 决定），无需 DynamicVar
    protected override void OnUpgrade() { }
}

[Pool(typeof(CasterCardPool))] public class EntrustedFuture() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int count = DynamicVars.Cards.IntValue;
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var chosen = await CardSelectCmd.FromCombatPile(ctx, exhaustPile, Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, count));
        foreach (var card in chosen) {
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
            // 原生的一站式「本回合免费打出」：同时把能量费和星费都清零。
            // 必须在进手牌**之后**调 —— 它内部走 AssertMutable，只对战斗内的可变实例有效（同 DistantHope 的顺序）。
            card.SetToFreeThisTurn();
        }
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class UnreceivedHairpin() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int x = ResolveEnergyXValue();
        int times = (x + 1) * DynamicVars.Repeat.IntValue;   // 基础 X+1、升级 2X+2，读的与卡面同一份声明
        for (int i = 0; i < times; i++)
            await CasterCardHelpers.SwitchForm(ctx, this);
    }
    protected override void OnUpgrade() { DynamicVars.Repeat.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class GiveStars() : MyFirstCharacterCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self) {
    /// <summary>升级文案里提到「对肃正防御」，按本仓库约定描述里提到的关键词都要在这里给提示。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<CasterInvinciblePower>()];

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
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(10m); }   // 50 -> 60
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

[Pool(typeof(CasterCardPool))] public class OnceSeenFinalStar() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Rare, TargetType.Self) {
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

[Pool(typeof(CasterCardPool))] public class AloneSeeksStars() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>升级后生成的是「独自一人+」，所以卡面提示也要按升级态取。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Alone>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        // Apply 在已有同名能力时返回的是**叠加后的同一个实例**，所以这里写的是
        // 「只要有一张是升级版，那一层加的「独自一人」之后都按升级版生成」。
        var power = await PowerCmd.Apply<AloneSeeksStarsPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);
        if (IsUpgraded && power != null) power.UpgradedAlone = true;
    }
    // 升级**不再减费**（费用保持 2），升级只体现为「生成的是独自一人+」—— 所以这里什么都不做
    protected override void OnUpgrade() { }
}

[Pool(typeof(CasterCardPool))] public class StarBirthMoment() : MyFirstCharacterCard(1, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block), HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    // 基础带「虚无」，升级后换成「保留」（清单里的关键字前缀已证实：基础行「虚无。」、升级行「保留。」）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<CasterDeathRiskPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { RemoveKeyword(CardKeyword.Ethereal); AddKeyword(CardKeyword.Retain); }
}

[Pool(typeof(CasterCardPool))] public class GloucesterDay11() : MyFirstCharacterCard(3, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<GloucesterDay11Power>(ctx, Owner.Creature, (int)DynamicVars.Block.BaseValue, Owner.Creature, this, false); }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(1m); }
}

[Pool(typeof(CasterCardPool))] public class FairyEnd() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Rare, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    // 描述里引用 {NpPercent:diff()}，必须在 CanonicalVars 声明，否则卡面渲染失败（SmartFormat 抛错）
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("NpPercent", 15m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        // 「每层百分比」直接当 Amount 存：基础 15、升级 20，叠两张 = 35%。
        // CasterCardHelpers.AfterSwitchForm 里读的就是这个 Amount，无需给 FairyEndPower 加字段。
        await PowerCmd.Apply<FairyEndPower>(ctx, Owner.Creature, IsUpgraded ? 20 : 15, Owner.Creature, this, false);
    }
    // 升级 +5%，且**不再**给「固有」（目标文案的升级行没有「固有。」）
    protected override void OnUpgrade() { DynamicVars["NpPercent"].UpgradeValueBy(5m); }
}

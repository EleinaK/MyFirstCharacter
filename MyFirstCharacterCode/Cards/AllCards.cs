using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MyFirstCharacter.MyFirstCharacterCode.Powers;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MegaCrit.Sts2.Core.HoverTips;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

public static class CasterCardHelpers
{
    public static async Task SwitchForm(PlayerChoiceContext ctx, CardModel card)
    {
        var c = card.Owner.Creature;
        if (c.HasPower<CasterFormOnePower>()) {
            await PowerCmd.Remove(c.GetPower<CasterFormOnePower>());
            await PowerCmd.Apply<CasterFormTwoPower>(ctx, c, 1, c, card, false);
            SpriteController.ApplyFrames(c, CasterFormTwoPower.FramesPath);
            await PowerCmd.Apply<StrengthPower>(ctx, c, CasterFormValues.FormTwoStrength(c), c, card, false);
            await PowerCmd.Apply<VulnerablePower>(ctx, c, CasterFormValues.FormTwoVulnerable(c), c, card, false);
        }
        else if (c.HasPower<CasterFormTwoPower>()) {
            await PowerCmd.Remove(c.GetPower<CasterFormTwoPower>());
            await PowerCmd.Apply<CasterFormOnePower>(ctx, c, 1, c, card, false);
            SpriteController.ApplyFrames(c, CasterFormOnePower.FramesPath);
            await NpCharge.Gain(c, CasterFormValues.FormOneNp(c));
            await PowerCmd.Apply<WeakPower>(ctx, c, 1, c, card, false);
        }
        else {
            // 无形态时默认进入「夏日之星」（形态二）
            await PowerCmd.Apply<CasterFormTwoPower>(ctx, c, 1, c, card, false);
            SpriteController.ApplyFrames(c, CasterFormTwoPower.FramesPath);
            await PowerCmd.Apply<StrengthPower>(ctx, c, CasterFormValues.FormTwoStrength(c), c, card, false);
            await PowerCmd.Apply<VulnerablePower>(ctx, c, CasterFormValues.FormTwoVulnerable(c), c, card, false);
        }

        // 标记「本回合切换过形态」
        await PowerCmd.Apply<CasterSwitchedFormPower>(ctx, c, 1, c, card, false);
        NpCharge.IncrementSwitchCount(c);
        await AfterSwitchForm(ctx, c);
    }

    /// <summary>切换形态后触发相关能力（花之庭园 / 形态流转 / 湖中仙女）。</summary>
    public static async Task AfterSwitchForm(PlayerChoiceContext ctx, Creature c)
    {
        if (c.Player == null) return;
        // 花之庭园：切换到圣剑使形态时获得能量（每层 1 点）
        var flowerGarden = c.GetPower<FlowerGardenPower>();
        if (c.HasPower<CasterFormOnePower>() && flowerGarden != null)
            await PlayerCmd.GainEnergy((int)flowerGarden.Amount, c.Player);
        // 形态流转：切换形态时抽牌（每层 1 张）
        var formFlow = c.GetPower<FormFlowPower>();
        if (formFlow != null)
            await CardPileCmd.Draw(ctx, (int)formFlow.Amount, c.Player);
        // 湖中仙女：切换形态时**随机**移除自身身上存在的一种负面效果（每层移除 1 种）。
        // 注意是随机取，不是按固定顺序取第一个 —— 原来写的是 FirstOrDefault(p => 虚弱/易伤/脆弱)，
        // 永远是先移除虚弱。
        var lakeFairy = c.GetPower<LakeFairyPower>();
        if (lakeFairy != null && c.Player != null) {
            var rng = c.Player.RunState.Rng.CombatCardSelection;
            for (int i = 0; i < (int)lakeFairy.Amount; i++) {
                var debuffs = c.Powers.Where(p => p is WeakPower or VulnerablePower or FrailPower).ToList();
                if (debuffs.Count == 0) break;
                await PowerCmd.Remove(debuffs[rng.NextInt(debuffs.Count)]);
            }
        }
        // 镜之氏族的毁灭：本回合费用降至0
        foreach (var card in PileType.Hand.GetPile(c.Player).Cards.Where(card => card is MirrorClanDestruction))
            card.EnergyCost.SetThisTurnOrUntilPlayed(0);
        // 救世之剑：每切换一次形态费用-2（本场战斗）
        foreach (var card in PileType.Hand.GetPile(c.Player).Cards.Where(card => card is SalvationSword))
            card.EnergyCost.SetThisCombat(Math.Max(0, 10 - 2 * NpCharge.SwitchCount(c)));
        // 妖精的终末：切换形态时所有敌人失去当前宝具值的一定比例生命（每层 20%，最少 1）
        var fairyEnd = c.GetPower<FairyEndPower>();
        if (fairyEnd != null) {
            int loss = Math.Max(1, (int)(NpCharge.Current(c) * 0.2m * fairyEnd.Amount));
            var enemies = c.CombatState?.Enemies.Where(e => e.IsAlive);
            if (enemies != null)
                foreach (var e in enemies)
                    await CreatureCmd.Damage(ctx, e, loss, ValueProp.Unpowered, c, null);
        }
    }

    /// <summary>Check if NP card should be generated. Called automatically by NpCharge.GaugeFilled event.</summary>
    public static async Task CheckNPThreshold(Creature c)
    {
        if (c.CombatState == null) return;
        var player = c.Player;
        if (player == null) return;

        // Only generate NP card if NP >= 100
        if (!NpCharge.IsOvercharged(c)) return;

        bool hasNP = false;
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
            foreach (var pc in pileType.GetPile(player).Cards)
                if (pc is CradlingYourHopeStar) { hasNP = true; break; }
        if (!hasNP)
        {
            var npCard = c.CombatState.CreateCard<CradlingYourHopeStar>(player);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(npCard, PileType.Hand, player));
        }
    }
}

// ===== STARTERS =====
[Pool(typeof(CasterCardPool))] public class BlueCard() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new DynamicVar("Np", 15m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx); await NpCharge.Gain(Owner.Creature, IsUpgraded ? 20 : 15); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); }
}
[Pool(typeof(CasterCardPool))] public class CasterStrike() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) {
    public override bool IsBasicStrikeOrDefend => true;
    // 游戏识别「基础打击/防御」主要靠 Tag，不是靠 IsBasicStrikeOrDefend：
    // 全 dll 里 Tags.Contains(CardTag.Strike/Defend) 约 20 处（先古事件的「变化一张打击」
    // 「移除 2 张打击」等选项的开启条件、CardPool.AllCards.First(...) 取基础打击/防御……），
    // 而 IsBasicStrikeOrDefend 只在 PandorasBox 一处被读。不声明 Tag 的话那些功能全部落空，
    // 其中 CardPool 那处用的是 First()，找不到会直接抛异常。
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Strike };
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx); }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); }
}
[Pool(typeof(CasterCardPool))] public class DefendCaster() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self) {
    public override bool GainsBlock => true;
    public override bool IsBasicStrikeOrDefend => true;
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Defend };
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play); }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); }
}
[Pool(typeof(CasterCardPool))] public class SummerBeach() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self) {
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await CasterCardHelpers.SwitchForm(ctx, this); }
    // 这张牌没有 CanonicalVars，所以不能用 DynamicVars.Energy：OnUpgrade 在卡牌显示时就会被调用
    // （BaseLib 的 UpgradeInternal patch ← NCardGrid.UpdateCardModel），抛 KeyNotFoundException
    // 会让整个牌堆/卡组界面构建失败、什么也看不到。降费要用 EnergyCost.UpgradeBy，
    // 跟 ParadiseFairy / FairyForm 的写法一致。
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}
[Pool(typeof(CasterCardPool))] public class LakeProtectionA() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 20m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await NpCharge.Gain(Owner.Creature, IsUpgraded ? 25 : 20); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); }
}
[Pool(typeof(CasterCardPool))] public class ExcaliburForgeEX() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Token, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 50m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await NpCharge.Gain(Owner.Creature, IsUpgraded ? 60 : 50); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(10m); }
}
[Pool(typeof(CasterCardPool))] public class EternalFeast() : MyFirstCharacterCard(2, CardType.Power, CardRarity.Ancient, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await PowerCmd.Apply<CasterEternalFeastPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); }
}

// ===== COMMONS =====
[Pool(typeof(CasterCardPool))] public class CaneStrike() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Dazed>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        var d1 = base.CombatState.CreateCard<Dazed>(Owner); CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(d1, PileType.Discard, Owner));
        var d2 = base.CombatState.CreateCard<Dazed>(Owner); CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(d2, PileType.Discard, Owner));
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); }
}
[Pool(typeof(CasterCardPool))] public class BasicMagicAttack() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new DynamicVar("Np", 5m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(base.CombatState).Execute(ctx);
        int npPer = IsUpgraded ? 10 : 5;
        await NpCharge.Gain(Owner.Creature,npPer * base.CombatState.Enemies.Count);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["Np"].UpgradeValueBy(5m); }
}
[Pool(typeof(CasterCardPool))] public class FlowerMageWand() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        if (IsUpgraded) { foreach (var c in PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable)) CardCmd.Upgrade(c); }
        else { var c = await CardSelectCmd.FromHandForUpgrade(ctx, Owner, this); if (c != null) CardCmd.Upgrade(c); }
    }
}
[Pool(typeof(CasterCardPool))] public class WilsonsGrove() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy) {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new RepeatVar(4)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var enemies = base.CombatState.Enemies.Where(e => e.IsAlive).ToList();
        var rng = Owner.PlayerRng;
        for (int i = 0; i < DynamicVars.Repeat.BaseValue; i++) {
            var target = enemies[Owner.PlayerRng.Rewards.NextInt(enemies.Count)];
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(ctx);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Repeat.UpgradeValueBy(1m); }
}
[Pool(typeof(CasterCardPool))] public class RingTheBell() : MyFirstCharacterCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(11m, ValueProp.Move), new PowerVar<WeakPower>(1m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx); await PowerCmd.Apply<WeakPower>(ctx, play.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this, false); EnergyCost.SetThisCombat(0); }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars.Weak.UpgradeValueBy(1m); }
}
[Pool(typeof(CasterCardPool))] public class HopeStar() : MyFirstCharacterCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) {
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move), new DynamicVar("Heal", 5m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(ctx);
        await CreatureCmd.Heal(Owner.Creature, IsUpgraded ? 7 : 5, true);
        await CasterCardHelpers.SwitchForm(ctx, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars["Heal"].UpgradeValueBy(2m); }
}
[Pool(typeof(CasterCardPool))] public class BasicMagicDefense() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Common, TargetType.Self) {
    /// <summary>宝具值：自定义关键字，提示文本在 card_keywords.json。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, ValueProp.Move), new DynamicVar("Np", 10m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        int npPerHit = IsUpgraded ? 15 : 10;
        // 不要「先移除再施加」—— 那会让第二张把第一张的效果顶掉。
        // CasterMagicDefensePower 现在是 Counter，叠加才对：打两张 = 本回合每次受到攻击得 2N。
        await PowerCmd.Apply<CasterMagicDefensePower>(ctx, Owner.Creature, npPerHit, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); DynamicVars["Np"].UpgradeValueBy(5m); }
}
[Pool(typeof(CasterCardPool))] public class SpringMemory() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 40m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await NpCharge.Gain(Owner.Creature, IsUpgraded ? 60 : 40); await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, -2, Owner.Creature, this, false); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(20m); }
}
[Pool(typeof(CasterCardPool))] public class FairyEye() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move), new PowerVar<WeakPower>(1m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<WeakPower>(ctx, play.Target, IsUpgraded ? 2 : 1, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); DynamicVars.Weak.UpgradeValueBy(1m); }
}
[Pool(typeof(CasterCardPool))] public class PoisonWine() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Common, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Infection>(), HoverTipFactory.FromKeyword(CasterKeywords.NpGauge), .. HoverTipFactory.FromCardWithCardHoverTips<CradlingYourHopeStar>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Np", 25m)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await NpCharge.Gain(Owner.Creature, IsUpgraded ? 30 : 25); var w = base.CombatState.CreateCard<Infection>(Owner); CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(w, PileType.Discard, Owner)); }
    protected override void OnUpgrade() { DynamicVars["Np"].UpgradeValueBy(5m); }
}
[Pool(typeof(CasterCardPool))] public class NeverGiveUp() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Wound>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CasterCardHelpers.SwitchForm(ctx, this);
        var c = base.CombatState.CreateCard<Wound>(Owner); CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(c, PileType.Discard, Owner));
        await PlayerCmd.GainEnergy(IsUpgraded ? 2 : 1, Owner);
    }
    protected override void OnUpgrade() { DynamicVars.Energy.UpgradeValueBy(1m); }
}
[Pool(typeof(CasterCardPool))] public class ThroughSnowstorm() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Common, TargetType.Self) {
    /// <summary>把衍生卡「CasterCalamity」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<CasterCalamity>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner); var s = base.CombatState.CreateCard<CasterCalamity>(Owner); CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(s, PileType.Discard, Owner)); }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); }
}
[Pool(typeof(CasterCardPool))] public class MicroAnomaly() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) {
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await CasterCardHelpers.SwitchForm(ctx, this); await PowerCmd.Apply<CasterMicroAnomalyPower>(ctx, Owner.Creature, 1, Owner.Creature, this, false); }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}
[Pool(typeof(CasterCardPool))] public class RaggedCloak() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Common, TargetType.Self) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Dazed>()];

    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) { await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play); var d = base.CombatState.CreateCard<Dazed>(Owner); CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(d, PileType.Discard, Owner)); }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); }
}
[Pool(typeof(CasterCardPool))] public class CalamityRetreat() : MyFirstCharacterCard(1, CardType.Skill, CardRarity.Common, TargetType.Self) {
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        var chosen = await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), c => c != (CardModel)this, this);
        var target = chosen.FirstOrDefault();
        if (target == null) return;
        int energyGain = (target.Type == CardType.Status || target.Type == CardType.Curse) ? 1 : target.EnergyCost.GetResolved();
        await CardCmd.Exhaust(ctx, target);
        await PlayerCmd.GainEnergy(energyGain, Owner);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}
// ===== NOBLE PHANTASM =====
[Pool(typeof(CasterCardPool))] public class CradlingYourHopeStar() : MyFirstCharacterCard(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) {
    /// <summary>描述里提到的关键词/状态牌，挂上悬停提示（标黄由描述里的 [gold] 负责，提示必须在这里给）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<FrailPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new EnergyVar(2), new DynamicVar("AoeMult", 2m), new DynamicVar("StMult", 3m)];

    /// <summary>
    /// 宝具卡，不该被随机生成出来：它 0 费，正好符合「大奖」(Jackpot) 挑 0 费牌的过滤条件，
    /// 而 FilterForCombat 只排除 Basic/Ancient/Event —— Token 不在排除列，所以实测会被大奖生成。
    /// 卡池那边还有一道 FilterThroughEpochs，负责「变化」等非战斗路径。
    /// </summary>
    public override bool CanBeGeneratedInCombat => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        if (!NpCharge.CanPay(Owner.Creature, 100)) return;
        await NpCharge.Spend(Owner.Creature, 100);
        NpCharge.IncrementNpCardsPlayed(Owner.Creature);

        // 不散之宴席：打出宝具卡时额外获得宝具值（每层 30 点）
        var feast = Owner.Creature.GetPower<CasterEternalFeastPower>();
        if (feast != null)
            await NpCharge.Gain(Owner.Creature, 30 * (int)feast.Amount);

        await DoNpEffect(ctx, play);
        // 予你的春之回忆：宝具卡的效果额外触发（每层多一次；第二次起都不再消耗宝具值）
        var doubleNp = Owner.Creature.GetPower<CasterDoubleNpPower>();
        if (doubleNp != null)
            for (int i = 0; i < (int)doubleNp.Amount; i++)
                await DoNpEffect(ctx, play);

        // 打出后若宝具值仍 >=100，补卡由 NpCharge.NpChanged 统一处理
        // （Spend(100) 会触发一次检查），这里不再手动生成，避免补两张。
    }

    private async Task DoNpEffect(PlayerChoiceContext ctx, CardPlay play)
    {
        // 消耗所有状态牌（手牌/抽牌堆/弃牌堆）。先快照，避免灾厄复制出的新牌被同一次结算消耗。
        var statuses = new List<CardModel>();
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
            statuses.AddRange(pileType.GetPile(Owner).Cards.Where(c => c.Type == CardType.Status));
        foreach (var c in statuses)
            await CardCmd.Exhaust(ctx, c);

        // 清除自己和同伴身上的易伤、虚弱、脆弱
        var allies = new List<Creature> { Owner.Creature };
        if (base.CombatState != null)
            allies.AddRange(base.CombatState.Allies.Where(a => a.IsAlive && a != Owner.Creature));
        foreach (var ally in allies) {
            var weak = ally.GetPower<WeakPower>();
            var vuln = ally.GetPower<VulnerablePower>();
            var frail = ally.GetPower<FrailPower>();
            if (weak != null) await PowerCmd.Remove(weak);
            if (vuln != null) await PowerCmd.Remove(vuln);
            if (frail != null) await PowerCmd.Remove(frail);
        }

        // 获得一层对肃正防御（下一次受到的伤害变为0，可叠加）
        await PowerCmd.Apply<CasterInvinciblePower>(ctx, Owner.Creature, 1, Owner.Creature, this, false);

        // 抽牌 + 回能
        int draw = IsUpgraded ? 3 : 2;
        await CardPileCmd.Draw(ctx, draw, Owner);
        await PlayerCmd.GainEnergy(IsUpgraded ? 3 : 2, Owner);

        // 伤害结算：全体 + 单体（1 + 消耗堆数量 × 倍率）
        int exhaustCount = (int)PileType.Exhaust.GetPile(Owner).Cards.Count();
        int aoeMult = IsUpgraded ? 3 : 2;
        int stMult = IsUpgraded ? 4 : 3;
        int aoeDmg = 1 + exhaustCount * aoeMult;
        int stDmg = 1 + exhaustCount * stMult;

        // 全体伤害（Hyperbeam）
        var beam = NHyperbeamVfx.Create(Owner.Creature, play.Target);
        if (beam != null)
            (NCombatRoom.Instance?.CombatVfxContainer as Node)?.AddChild(beam);
        await Cmd.Wait(0.15f);
        await DamageCmd.Attack((decimal)aoeDmg).FromCard(this).TargetingAllOpponents(base.CombatState)
            .WithHitVfxNode(target => NHyperbeamImpactVfx.Create(Owner.Creature, target))
            .Execute(ctx);

        // 单体伤害（Grand Finale）
        await Cmd.Wait(0.25f);
        var gf = NGrandFinaleVfx.Create(Owner.Creature);
        if (gf != null)
            (NCombatRoom.Instance?.CombatVfxContainer as Node)?.AddChild(gf);
        await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
        await DamageCmd.Attack((decimal)stDmg).FromCard(this).Targeting(play.Target)
            .WithHitVfxNode(NGrandFinaleImpactVfx.Create)
            .Execute(ctx);
    }
    protected override void OnUpgrade() { DynamicVars.Cards.UpgradeValueBy(1m); DynamicVars.Energy.UpgradeValueBy(1m); DynamicVars["AoeMult"].UpgradeValueBy(1m); DynamicVars["StMult"].UpgradeValueBy(1m); }
}

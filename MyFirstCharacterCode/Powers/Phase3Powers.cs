using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

// 叠加规则（2026-10-06 定）：会重复触发的「能力」一律用 Counter，让 Amount 累加，
// 钩子里按「整个效果重复 Amount 次」实现。只有二值状态的标记类才留 Single。
// 26 个 Power 里原来 22 个是 Single，导致同一张能力卡打多张完全不叠加。

/// <summary>标记：本回合切换过形态（回合结束移除）。二值状态，保持 Single。</summary>
public class CasterSwitchedFormPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }

    /// <summary>本回合切换过形态时，之后才进手牌的镜之氏族的毁灭也要变成 0 费。</summary>
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner == null || card.Owner.Creature != Owner) return Task.CompletedTask;
        if (Owner.Player == null) return Task.CompletedTask;
        foreach (var c in PileType.Hand.GetPile(Owner.Player).Cards)
            if (c is MyFirstCharacter.MyFirstCharacterCode.Cards.MirrorClanDestruction)
                c.EnergyCost.SetThisTurnOrUntilPlayed(0);
        return Task.CompletedTask;
    }
}

/// <summary>妖精的舞步：本回合每打出一张技能牌获得格挡（回合结束移除）。</summary>
public class FairyDancePower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type != CardType.Skill) return;
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}

/// <summary>星之护壁：回合开始时获得等同于10%宝具值的格挡（每层各自一份，即 Amount × 10%）。</summary>
public class StarBarrierPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        int block = (int)(NpCharge.Current(Owner) * 0.1m) * (int)Amount;
        if (block > 0)
            await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}

/// <summary>花之庭园：切换到圣剑使形态时获得能量（触发在 SwitchForm，每层1点）。</summary>
public class FlowerGardenPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>镜之氏族的盟约：每消耗1张状态牌抽牌（每层1张）。</summary>
public class MirrorClanCovenantPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner.Creature != Owner) return;
        if (card.Type != CardType.Status) return;
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}

/// <summary>灾厄转化：每生成1张状态牌获得宝具值。</summary>
public class CalamityConversionPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card.Type != CardType.Status) return;
        if (creator?.Creature != Owner) return;
        await NpCharge.Gain(Owner, (int)Amount);
    }
}

/// <summary>消耗炼成：每消耗1张牌获得宝具值。</summary>
public class ExhaustRefinementPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner.Creature != Owner) return;
        await NpCharge.Gain(Owner, (int)Amount);
    }
}

/// <summary>形态流转：切换形态时抽牌（触发在 SwitchForm，抽 Amount 张）。</summary>
public class FormFlowPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>留存心中的星之悸动：使敌人本回合失去力量（临时力量下降，回合结束回退）。</summary>
public class KeptStarThrobPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<MyFirstCharacter.MyFirstCharacterCode.Cards.KeptStarThrob>();
    protected override bool IsPositive => false;
}

/// <summary>
/// 吾乃撕裂星辰之雷霆：回合开始时将1张眩晕加入手牌，并选择1张手牌变化为随机牌。
/// 每层完整重复一次（打 2 张 → 加 2 张眩晕 + 变 2 张手卡）。
/// </summary>
public class StarRendingThunderPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.CombatState == null) return;
        int times = (int)Amount;
        for (int i = 0; i < times; i++)
        {
            var dazed = Owner.CombatState.CreateCard<Dazed>(player);
            await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Hand, player);
            var hand = PileType.Hand.GetPile(player).Cards.Where(c => c != (CardModel)dazed).ToList();
            if (hand.Count == 0) continue;
            var chosen = await CardSelectCmd.FromHand(choiceContext, player, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1), c => c != (CardModel)dazed, this);
            foreach (var card in chosen)
                await CardCmd.TransformToRandom(card, Owner.Player.RunState.Rng.CombatCardSelection, CardPreviewStyle.None);
        }
    }
}

/// <summary>湖中仙女：切换形态时移除自身负面效果（触发在 SwitchForm，每层移除 1 种）。</summary>
public class LakeFairyPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

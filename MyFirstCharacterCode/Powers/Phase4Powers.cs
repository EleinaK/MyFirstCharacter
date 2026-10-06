using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

// 叠加规则同 Phase3Powers：会重复触发的能力用 Counter，钩子按「整个效果重复 Amount 次」实现。

/// <summary>乐园妖精：回合开始时从消耗牌堆选择加入手牌（每层1张）。</summary>
public class ParadiseFairyPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        int times = (int)Amount;
        for (int i = 0; i < times; i++)
        {
            var exhaustPile = PileType.Exhaust.GetPile(player);
            if (exhaustPile.Cards.Count == 0) break;
            var chosen = await CardSelectCmd.FromCombatPile(choiceContext, exhaustPile, player, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1));
            foreach (var card in chosen)
                await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
        }
    }
}

/// <summary>巡礼之旅：回合开始时选择手牌消耗（每层1张）。</summary>
public class PilgrimagePower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        int times = (int)Amount;
        for (int i = 0; i < times; i++)
        {
            var hand = PileType.Hand.GetPile(player).Cards.ToList();
            if (hand.Count == 0) break;
            var chosen = await CardSelectCmd.FromHand(choiceContext, player, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), c => true, this);
            foreach (var card in chosen)
                await CardCmd.Exhaust(choiceContext, card);
        }
    }
}

/// <summary>就算独自一人也会寻觅星辰：回合开始时将「独自一人」加入手牌（每层1张）。</summary>
public class AloneSeeksStarsPower : MyFirstCharacterPower
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
            var alone = Owner.CombatState.CreateCard<MyFirstCharacter.MyFirstCharacterCode.Cards.Alone>(player);
            await CardPileCmd.AddGeneratedCardToCombat(alone, PileType.Hand, player);
        }
    }
}

/// <summary>那是曾经见过的终焉之星：回合开始时抽牌（每层1张）。</summary>
public class OnceSeenFinalStarPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}

/// <summary>第11天的格洛斯特：每给予敌人易伤时获得格挡。</summary>
public class GloucesterDay11Power : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        if (power is VulnerablePower && power.Owner != null && power.Owner != Owner)
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>妖精形态：每给予敌人易伤时获得力量（每层1点）。</summary>
public class FairyFormPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        if (power is VulnerablePower && power.Owner != null && power.Owner != Owner)
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, null, false);
    }
}

/// <summary>妖精的终末：切换形态时所有敌人失去当前宝具值的一定比例生命（触发在 SwitchForm，每层20%）。</summary>
public class FairyEndPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>予你的春之回忆：宝具卡的效果额外触发（触发在宝具卡 OnPlay，每层多一次）。</summary>
public class CasterDoubleNpPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>圣剑拔刀：本回合+临时力量（回合结束回退）。</summary>
public class BladeDrawStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<MyFirstCharacter.MyFirstCharacterCode.Cards.BladeDraw>();
}

/// <summary>星辰诞生之刻：每打出一张牌获得宝具值（每层10点）；若本场战斗受到未格挡的攻击伤害则立刻死亡。</summary>
public class CasterDeathRiskPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        await NpCharge.Gain(Owner, 10 * (int)Amount);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!props.IsPoweredAttack()) return;
        if (result.UnblockedDamage <= 0) return;
        await CreatureCmd.Kill(Owner, true);
    }
}

/// <summary>此身为剑所天成：每打出一张技能牌获得宝具值。</summary>
public class BodyIsSwordPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type != CardType.Skill) return;
        await NpCharge.Gain(Owner, (int)Amount);
    }
}

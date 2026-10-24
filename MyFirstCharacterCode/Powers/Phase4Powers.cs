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

    /// <summary>
    /// 来源卡是升级版时，生成的「独自一人」也带升级。
    /// 普通字段而非 SpireField：PowerModel.ToMutable() 走 MemberwiseClone，字段会被带过去；
    /// 但存档/联机快照不序列化它 —— 战斗中读档会退回未升级版，这是可接受的降级。
    /// </summary>
    public bool UpgradedAlone;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.CombatState == null) return;
        int times = (int)Amount;
        for (int i = 0; i < times; i++)
        {
            var alone = Owner.CombatState.CreateCard<MyFirstCharacter.MyFirstCharacterCode.Cards.Alone>(player);
            if (UpgradedAlone) CardCmd.Upgrade(alone);
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

/// <summary>第11天的格洛斯特：每当你给予敌人易伤时获得格挡。</summary>
public class GloucesterDay11Power : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    // 不能用 AfterModifyingPowerAmountGiven：那个钩子只派发给「本次实际修改了层数的 modifier」
    // （Hook.ModifyPowerAmountGiven 的 out modifiers 列表），本能力不改任何层数 →
    // 永远不会被调用（旧实现的 bug：夏日之星形态每张牌自动给敌人挂易伤时，格挡完全不触发）。
    // AfterPowerAmountChanged 才是「任何能力层数变化后通知所有 hook 监听者」的通用钩子。
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0) return;                                                        // 只处理施加/叠层，忽略回合末衰减
        if (power is not VulnerablePower || power.Owner == null || power.Owner == Owner) return;  // 只认敌人身上的易伤
        bool givenByOwner = applier == Owner || (applier == null && cardSource?.Owner?.Creature == Owner);
        if (!givenByOwner) return;                                                      // 只认「你给予的」，敌方互相挂易伤不触发
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

/// <summary>妖精形态：每当你给予敌人易伤时获得力量（每层1点）。</summary>
public class FairyFormPower : MyFirstCharacterPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    // 同 GloucesterDay11Power：旧实现用 AfterModifyingPowerAmountGiven，永远不会触发。
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0) return;
        if (power is not VulnerablePower || power.Owner == null || power.Owner == Owner) return;
        bool givenByOwner = applier == Owner || (applier == null && cardSource?.Owner?.Creature == Owner);
        if (!givenByOwner) return;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null, false);
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

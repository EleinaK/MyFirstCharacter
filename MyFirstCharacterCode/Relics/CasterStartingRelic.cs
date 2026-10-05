using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MyFirstCharacter.MyFirstCharacterCode.Cards;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;
using MyFirstCharacter.MyFirstCharacterCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace MyFirstCharacter.MyFirstCharacterCode.Relics;

[Pool(typeof(CasterRelicPool))]
public class CasterStartingRelic : MyFirstCharacterRelic
{
    /// <summary>把衍生卡「CradlingYourHopeStar」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<CradlingYourHopeStar>()];

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShouldReceiveCombatHooks => true;

    /// <summary>先古事件强化：替换为先古遗物「不再遥远的希望之星」。</summary>
    public override RelicModel? GetUpgradeReplacement()
    {
        return ModelDb.Relic<AncientHopeStarRelic>();
    }

    public override async Task BeforeCombatStartLate()
    {
        await base.BeforeCombatStartLate();
        var c = Owner.Creature;
        await NpCharge.BeginCombat(c);
        // 初始形态改为「夏日之星」（形态二）
        if (!c.HasPower<CasterFormOnePower>() && !c.HasPower<CasterFormTwoPower>())
        {
            await PowerCmd.Apply<CasterFormTwoPower>(new BlockingPlayerChoiceContext(), c, 1, c, null, false);
            SpriteController.ApplyFrames(c, CasterFormTwoPower.FramesPath);
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), c, CasterFormValues.FormTwoStrength(c), c, null, false);
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), c, CasterFormValues.FormTwoVulnerable(c), c, null, false);
            // 恢复上一场战斗继承的宝具值显示条，并检查是否已过阈值
            await NpCharge.SyncDisplay(c);
            await CasterCardHelpers.CheckNPThreshold(c);
        }
    }

    /// <summary>记录「本回合消耗过牌」，供预言之子的旅途等卡判断。</summary>
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

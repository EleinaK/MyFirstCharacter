using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

/// <summary>
/// 独自一人（无色牌）：保留、消耗。切换为另一种形态。升级后移除「消耗」。
/// </summary>
[Pool(typeof(CasterCardPool))]
public class Alone() : MyFirstCharacterCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    /// <summary>
    /// 不该被随机生成出来。大奖(Jackpot)/攻击药水/Discovery 等都走 CardFactory.FilterForCombat，
    /// 它只排除 Basic/Ancient/Event —— Token 不在排除列，所以 0 费的这张会被抽中。
    /// 游戏对自家状态牌也是这么挡的（见原生 Disintegration）。
    /// 卡池那边还有一道 FilterThroughEpochs，负责「变化」等非战斗路径。
    /// </summary>
    public override bool CanBeGeneratedInCombat => false;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CasterCardHelpers.SwitchForm(ctx, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

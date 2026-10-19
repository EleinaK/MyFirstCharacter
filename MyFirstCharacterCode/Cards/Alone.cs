using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MyFirstCharacter.MyFirstCharacterCode.Cards;

/// <summary>
/// 独自一人（无色牌）：保留、消耗。切换一次形态；升级后仍是保留+消耗，改为切换三次。
/// </summary>
/// <remarks>
/// 升级**不再**去掉「消耗」（去掉 OnUpgrade 覆写即可），改成切换三次。
/// 注意 SwitchForm 是「翻转」而不是「选择形态」，且每次都跑 CasterCardHelpers.AfterSwitchForm ——
/// 三连切 = 妖精的终末结算 3 次（切进形态一会加宝具值，所以越切越痛）、形态流转抽 3 轮、
/// 花之庭园按进入形态一的次数多次给能量、崩落按累计切换数减费、本回合力量/易伤/虚弱的形态加成各叠 3 次。
/// 这是设计使然。
/// </remarks>
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
        int times = IsUpgraded ? 3 : 1;
        for (int i = 0; i < times; i++)
            await CasterCardHelpers.SwitchForm(ctx, this);
    }
}

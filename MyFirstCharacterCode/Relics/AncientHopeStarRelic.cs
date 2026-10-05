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

/// <summary>先古遗物「不再遥远的希望之星」：替换起始遗物，形态入场效果强化（+30NP / +4力量）。</summary>
[Pool(typeof(CasterRelicPool))]
public class AncientHopeStarRelic : MyFirstCharacterRelic
{
    /// <summary>把衍生卡「CradlingYourHopeStar」的卡面挂到悬停提示上（同游戏原生的压缩→燃料）。
    /// 产生的是未升级的衍生卡，所以不传 upgrade 参数。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<CradlingYourHopeStar>()];

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;

    // 图标不复写，走基类按 id 算出来的路径（MyFirstCharacterRelic 里）：
    //   images/relics/ancient_hope_star_relic.png + 同名 _outline.png + big/ 里的同名大图。
    // 原来这里复写了 PackedIconPath => "casterstartingrelic.png"（想复用起始遗物图标），
    // 但那个文件名是错的 —— 磁盘上是 caster_starting_relic.png（带下划线）。
    // RelicImagePath() 找不到就回退到 relic.png，那是个 1x1 像素的占位图，
    // 于是这个遗物的图标「看不见」。删掉复写即用上专画的 ancient_hope_star_relic.png。
    // 注：ancient_hope_star_relic_outline.png 不存在（其余遗物的 _outline 也都是 1x1 占位），
    // 会走同样的回退 + 记一条 Info 日志，功能上无影响。

    public override async Task BeforeCombatStartLate()
    {
        await base.BeforeCombatStartLate();
        var c = Owner.Creature;
        await NpCharge.BeginCombat(c);
        if (!c.HasPower<CasterFormOnePower>() && !c.HasPower<CasterFormTwoPower>())
        {
            await PowerCmd.Apply<CasterFormTwoPower>(new BlockingPlayerChoiceContext(), c, 1, c, null, false);
            SpriteController.ApplyFrames(c, CasterFormTwoPower.FramesPath);
            await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), c, CasterFormValues.FormTwoStrength(c), c, null, false);
            await PowerCmd.Apply<VulnerablePower>(new BlockingPlayerChoiceContext(), c, CasterFormValues.FormTwoVulnerable(c), c, null, false);
            await NpCharge.SyncDisplay(c);
            await CasterCardHelpers.CheckNPThreshold(c);
        }
    }

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

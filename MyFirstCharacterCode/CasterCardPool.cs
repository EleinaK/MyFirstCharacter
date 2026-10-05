using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using MyFirstCharacter.MyFirstCharacterCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace MyFirstCharacter.MyFirstCharacterCode;

public class CasterCardPool : CustomCardPoolModel
{
    public override string Title => "MyFirstCharacter";
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
    public override float H => 0.6f;
    public override float S => 0.5f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => new("4a90d9");
    public override bool IsColorless => false;

    /// <summary>
    /// 卡池对外提供的「可获得卡」里剔除 Token / Status。
    ///
    /// 为什么必须在卡池挡：本 mod 的 token/status 卡（独自一人、宝具卡、灾厄）必须挂在角色卡池里
    /// —— BaseLib 的注册模型强制要求每张卡带 [Pool] 属性（见 CustomContentDictionary.AddModel，
    /// 没有就 throw），所以它们会出现在 CardPool.GetUnlockedCards() 里，而游戏的随机生成与变化
    /// 全都从那里取牌。实测两个漏点：
    ///   1) 生成：卡牌「大奖」(Jackpot) 用 CardFactory.GetForCombat(...) 抽 0 费非 X 的牌，
    ///      而 FilterForCombat 只排除 Basic/Ancient/Event —— Token/Status 不在排除列，
    ///      于是会抽出 0 费的「环抱着你的希望之星」和「独自一人」。
    ///   2) 变化：GetFilteredTransformationOptions 只在**原卡不是 Status/Curse** 时才把目标限制在
    ///      Common/Uncommon/Rare；变化一张状态牌（例如灾厄自己）时 Token/Status 会成为合法目标。
    ///
    /// 卡上另有 CanBeGeneratedInCombat = false（游戏对自家状态牌就是这么做的，见原生 Disintegration），
    /// 两者互补：卡上那个只管战斗内的生成效果，这里管所有走卡池的路径。
    ///
    /// 副作用：卡牌图鉴/收藏界面也读 GetUnlockedCards，所以这三张不会出现在图鉴列表里。
    /// 若想让它们照常出现在图鉴、只挡住生成与变化，就删掉这个覆写、只留卡上的开关
    /// （代价是留着上面第 2 个漏点）。
    /// </summary>
    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        return base.FilterThroughEpochs(unlockState, cards)
            .Where(c => c.Rarity != CardRarity.Token && c.Rarity != CardRarity.Status);
    }
}

using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.addons.mega_text;
using MyFirstCharacter.MyFirstCharacterCode.Cards;

namespace MyFirstCharacter.MyFirstCharacterCode.Patches;

/// <summary>
/// 让白名单里的 9 张卡借用游戏「先古卡」的全卡面渲染：隐藏普通卡框 / 立绘 / 横幅，改用铺满
/// 整卡的 AncientPortrait，再叠先古边框、文字底与火焰横幅。完全不碰 CardRarity —— 稀有度同时是
/// 奖励池、起始卡组与升级规则的输入，改了会把卡从普通奖励池里摘掉。
///
/// 游戏自己的先古分支在 NCard.Reload() 里按 Model.Rarity == CardRarity.Ancient 开关这些节点
/// （唯一名见 scenes/cards/card.tscn）。这里用后置补丁对白名单复刻同样的开关：先古的边框 / 文字底
/// 是卡牌场景里按稀有度开关的节点，BaseLib 暴露的钩子（CustomFrame / 材质 / ShaderColor）碰不到。
///
/// 尺寸依据（2026-10-05 从游戏 pck 里实测）：
///   - 卡本体 300x422（比例 0.711），AncientPortrait 节点 598x842（比例 0.710），即铺满整卡。
///   - ancient_card_border.tres 区域 821x1148（0.715），ancient_portrait_mask_large.tres 600x847。
///   - 立绘源图 512x875（0.585）由节点 KeepAspectCovered 居中裁掉上下各约 8.8%；512x724（0.707）
///     几乎不用裁。参见 .tools/convert_cardart.py 的 FULL_IMAGE_IDS。
/// </summary>
public static class AncientCardLookPatch
{
    private static readonly HashSet<Type> FullArtCards =
    [
        typeof(LakeProtectionA), typeof(HolySwordFlash), typeof(RepayAll),
        typeof(BasicMagicAttack), typeof(BasicMagicDefense), typeof(StarCharge),
        typeof(ProphecyLight), typeof(FlowerMageWand), typeof(CradlingYourHopeStar),
    ];

    // 路径取自 CardModel.AncientBorderPath / AncientTextBgPath。这两个字符串必须与游戏逐字一致：
    // ".sprites/*.tres" 由游戏的 AtlasResourceLoader 按「图集名 + 精灵名」在运行时注册表里解析
    // （读 res://images/atlases/<图集>.tpsheet），不是按文件路径读的。
    private const string BorderInnerPath = "atlases/compressed_atlas.sprites/ancient_card_border.png.tres";
    private const string TextBgInnerFormat = "atlases/compressed_atlas.sprites/ancient_text_bg_{0}.png.tres";

    // 可见的先古卡由 Reload() 给 PortraitCanvasGroup 套上遮罩材质（把立绘裁成先古卡轮廓）。
    // 非先古的可见卡该槽位是 null，所以这里必须自己设，否则整卡立绘是一个硬边矩形。
    private const string PortraitMaskMaterialPath = "res://scenes/cards/card_canvas_group_mask_material.tres";

    /// <summary>宝具卡的「醒目」金色。乘算到先古边框与横幅上，实装后按肉眼微调。</summary>
    private static readonly Color RareGold = new("FFD24A");

    // 与 NCard 的 _canvasGroupMaskMaterial 同款：静态缓存，避免每次 Reload 都查一遍资源表
    private static Material? _maskMaterial;

    private static Material? MaskMaterial
    {
        get
        {
            if (_maskMaterial == null || !GodotObject.IsInstanceValid(_maskMaterial))
                _maskMaterial = PreloadManager.Cache.GetMaterial(PortraitMaskMaterialPath);
            return _maskMaterial;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "Reload")]
    public static void Reload_Postfix(NCard __instance) => Apply(__instance);

    // ReloadOverlay 是 Reload 的最后一步，别处也会单独调它（例如加异常状态时）；再补一次，
    // 防止那次调用把可见性冲回普通外观。
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "ReloadOverlay")]
    public static void ReloadOverlay_Postfix(NCard __instance) => Apply(__instance);

    private static void Apply(NCard card)
    {
        CardModel? model = card.Model;
        if (model == null || !FullArtCards.Contains(model.GetType())) return;

        // 未解锁 / 未见过交给游戏原本的处理（它会给立绘加模糊），不套先古外观。
        // 实测这个状态只出现在卡牌图鉴界面（GetCardVisibility 只在那里被覆写），
        // 手牌 / 牌组 / 奖励界面传给 NCard.Create 的都是默认的 Visible。
        if (card.Visibility != ModelVisibility.Visible) return;

        TextureRect? portrait        = card.GetNodeOrNull<TextureRect>("%Portrait");
        TextureRect? portraitBorder  = card.GetNodeOrNull<TextureRect>("%PortraitBorder");
        TextureRect? frame           = card.GetNodeOrNull<TextureRect>("%Frame");
        TextureRect? banner          = card.GetNodeOrNull<TextureRect>("%TitleBanner");
        TextureRect? ancientPortrait = card.GetNodeOrNull<TextureRect>("%AncientPortrait");
        TextureRect? ancientBorder   = card.GetNodeOrNull<TextureRect>("%AncientBorder");
        TextureRect? ancientTextBg   = card.GetNodeOrNull<TextureRect>("%AncientTextBg");
        Control? ancientBanner       = card.GetNodeOrNull<Control>("%AncientBanner");
        TextureRect? ancientGlass    = card.GetNodeOrNull<TextureRect>("%AncientBorderGlassOverlay");
        CanvasGroup? canvasGroup     = card.GetNodeOrNull<CanvasGroup>("%PortraitCanvasGroup");

        // 游戏更新改了节点名就整体退回普通外观，不做半吊子渲染
        if (ancientPortrait == null || ancientBorder == null || ancientTextBg == null) return;

        if (portrait != null) portrait.Visible = false;
        if (portraitBorder != null) portraitBorder.Visible = false;
        if (frame != null) frame.Visible = false;
        if (banner != null) banner.Visible = false;

        ancientPortrait.Visible = true;
        ancientPortrait.Texture = model.Portrait;
        // 源图比卡面更瘦长：铺满优先，多出来的部分上下均分裁掉（场景默认是拉伸，会把人脸压扁）
        ancientPortrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        ancientPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;

        ancientBorder.Visible = true;
        ancientBorder.Texture = model.AncientBorder;

        ancientTextBg.Visible = true;
        ancientTextBg.Texture = ResourceLoader.Load<Texture2D>(
            ImageHelper.GetImagePath(string.Format(TextBgInnerFormat, TypeToken(model.Type))),
            null, ResourceLoader.CacheMode.Reuse);

        if (ancientBanner != null) ancientBanner.Visible = true;
        if (ancientGlass != null) ancientGlass.Visible = true;

        if (canvasGroup != null) canvasGroup.Material = MaskMaterial;

        if (model is CradlingYourHopeStar) ApplyRareGold(card, ancientBorder, ancientBanner);
    }

    private static void ApplyRareGold(NCard card, TextureRect border, Control? banner)
    {
        // 用 Modulate 而不是 SelfModulate：该节点在 card.tscn 里是
        //     modulate = Color(1, 0.9777, 0.9058, 0.502)   + CanvasItemMaterial(blend_mode = ADD)
        // ADD 混合的语义是「把 贴图 × 颜色 加到画面上」，所以乘法只能减光 —— 只染金不减 alpha
        // 会把它弄暗（这是第一版看起来没变化的原因）。这里用 Modulate 顶掉那个 0.502 的 alpha
        // （投光翻倍）再染金，才会既更亮又偏金。该节点无子节点，Modulate 不会外溢。
        border.Modulate = new Color(RareGold, 1f);

        if (banner != null)
            foreach (Node child in banner.GetChildren())
                if (child is CanvasItem ci) ci.SelfModulate = RareGold;

        // 标题用亮金（StsColors.gold = EFC851）。原方案用的 cardTitleOutlineRare = 6B4B00
        // 是暗棕金，在卡名上几乎看不出来。升级时游戏用绿色表示「已升级」，那种情况不覆盖。
        MegaLabel? title = card.GetNodeOrNull<MegaLabel>("%TitleLabel");
        if (title != null && card.Model?.CurrentUpgradeLevel == 0)
        {
            title.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, StsColors.gold);
            title.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.gold);
        }
    }

    /// <summary>取法与游戏 AncientTextBgPath 一致：Status/Curse/None 一律当作 Skill。</summary>
    private static string TypeToken(CardType type) => type switch
    {
        CardType.Attack => "attack",
        CardType.Power => "power",
        CardType.Quest => "quest",
        _ => "skill",
    };
}

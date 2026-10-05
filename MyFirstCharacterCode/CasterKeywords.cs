using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MyFirstCharacter.MyFirstCharacterCode;

/// <summary>
/// 本 mod 的自定义关键字。写法照抄 BaseLib 自己的 BaseLibKeywords：
/// 声明一个 CardKeyword 类型的静态字段，配 [CustomEnum(null)] 让 BaseLib 把它注册进扩展枚举，
/// [KeywordProperties] 决定要不要自动附加到卡牌描述（这里用 None —— 我们是在描述里手写
/// [gold]宝具值[/gold]，不需要自动附加）。
/// 提示文本按字段名的大写蛇形到 localization/{lang}/card_keywords.json 查：
/// NP_GAUGE.title / NP_GAUGE.description。
/// </summary>
public static class CasterKeywords
{
    [CustomEnum(null)]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword NpGauge;
}

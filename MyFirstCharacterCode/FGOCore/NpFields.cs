using BaseLib.Utils;
using MyFirstCharacter.MyFirstCharacterCode.Relics;

namespace MyFirstCharacter.MyFirstCharacterCode.FGOCore;

/// <summary>
/// 局内自定义状态的真实存储：挂在「星之内海的圣剑使」（或其先古强化版「不再遥远的希望之星」）遗物上，
/// 随存档和多人权威快照一起走。原先这些都放在进程内的静态字典里，重连方进程一重启就全没了。
///
/// 为什么不能用 Player 挂：BaseLib 的 SavePatchUtils.IsHolderTypeBaseSupported 只认
/// RelicModel / CardModel / EnchantmentModel / ModifierModel；而且 Player.SyncWithSerializedPlayer
/// 是**手工逐字段复制**、不调 Player.FromSerializable，挂在 Player 上的值即使进了包，收到后也会被丢掉。
/// 遗物那条路是完整的：SyncWithSerializedPlayer → PopulateRelics → RelicModel.FromSerializable（BaseLib 补过）。
///
/// 为什么不用原生 [SavedProperty]：v0.107.1 里 SavedPropertiesTypeCache.InjectTypeIntoCache 全 dll
/// 零调用点，属性名→网络 ID 按插入顺序分配、跨类型不排序（0.108.0 才修）。BaseLib 的
/// SavedSpireFieldPatch 用 AddFieldsSorted() 排序后注入名字，正是这个坑的解药。
///
/// TKey 用自己的抽象基类而不是 RelicModel：SavedSpireFieldPatch 按 TargetType.IsInstanceOfType 过滤，
/// 用 RelicModel 会把这四个 int 写进**全游戏每个遗物**（含别的 mod 的）的 Props 里，每次同步和存档都带着。
///
/// 运行时名字是 TKey 类型名 + 字段名（BaseLib 自己拼的前缀），即 "MyFirstCharacterRelic_np" 等 ——
/// 排查时在 godot.log 里 grep 这四个。
/// </summary>
internal static class NpFields
{
    /// <summary>
    /// 「未设置」哨兵。必须是**非 null 的装箱 int**：SpireField.Get 对未设置的值会做 (TVal)value 拆箱，
    /// 默认值给 null 会直接 NRE。同时它也是「这个遗物还没被写过值」的判据。
    /// </summary>
    public const int Unset = -1;

    public static readonly SavedSpireField<MyFirstCharacterRelic, int> Np = new(() => Unset, "np");
    public static readonly SavedSpireField<MyFirstCharacterRelic, int> NpCardsPlayed = new(() => Unset, "npCardsPlayed");
    public static readonly SavedSpireField<MyFirstCharacterRelic, int> SwitchCount = new(() => Unset, "switchCount");
    public static readonly SavedSpireField<MyFirstCharacterRelic, int> GrowingBladeBonus = new(() => Unset, "growingBladeBonus");

    /// <summary>
    /// 强制跑一次静态构造器：SavedSpireField 是在自己的构造器里完成注册的
    /// （SavedSpireFieldPatch.Register 是 internal，mod 直接调不到）。BaseLib 的 LatePostInit 也会扫一遍，
    /// 这里显式来一下，就不依赖反射可达性了。
    /// </summary>
    /// <summary>四个字段在运行时用的网络名（BaseLib 用 TKey 类型名 + 字段名拼的）。
    /// 排查时拿它和 BaseLib 的 "Added saved property name to basegame cache: X => N" 对。</summary>
    public static string[] RegisteredNames() =>
    [
        $"{typeof(MyFirstCharacterRelic).Name}_np",
        $"{typeof(MyFirstCharacterRelic).Name}_npCardsPlayed",
        $"{typeof(MyFirstCharacterRelic).Name}_switchCount",
        $"{typeof(MyFirstCharacterRelic).Name}_growingBladeBonus",
    ];

    public static void EnsureRegistered()
    {
        _ = Np;
        _ = NpCardsPlayed;
        _ = SwitchCount;
        _ = GrowingBladeBonus;
    }
}

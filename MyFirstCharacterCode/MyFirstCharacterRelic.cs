using BaseLib.Abstracts;
using BaseLib.Extensions;
using MyFirstCharacter.MyFirstCharacterCode.Extensions;
using MyFirstCharacter.MyFirstCharacterCode.FGOCore;

namespace MyFirstCharacter.MyFirstCharacterCode.Relics;

/// <summary>
/// This is the base class for your mod's relics, which is set up to load the relic's images from your mod's resources.
/// When creating a relic, right click the Relics folder and create a new file with the Custom Relic template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
/// </summary>
public abstract class MyFirstCharacterRelic : CustomRelicModel
{
    //MyFirstCharacter/images/relics
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();

    public override List<(string, string)>? Localization => CasterLocalization.ForEntry(Id.Entry, "relics");

    // 宝具值等局内状态存在遗物上（见 FGOCore/NpFields），所以遗物被替换时要把它搬过去。
    // 先古事件走 RelicCmd.Replace = Remove(原) → Obtain(新, 原索引)，正好是下面这一对钩子。
    //
    // 注意：**不要**把这段挪进 GetUpgradeReplacement() —— 那个方法会被 NRelicCollectionCategory.LoadRelics
    // 用规范实例调用，Owner 会因 AssertMutable 抛异常。

    public override Task AfterRemoved()
    {
        NpRelic.Stash(this);
        return Task.CompletedTask;
    }

    public override Task AfterObtained()
    {
        NpRelic.Restore(this);
        return Task.CompletedTask;
    }
}
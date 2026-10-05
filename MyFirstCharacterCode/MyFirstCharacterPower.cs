using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MyFirstCharacter.MyFirstCharacterCode.Extensions;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

public abstract class MyFirstCharacterPower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
    public override string CustomBigIconPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    public abstract override PowerType Type { get; }
    public abstract override PowerStackType StackType { get; }

    public override List<(string, string)>? Localization => CasterLocalization.ForEntry(Id.Entry, "powers");
}

using MegaCrit.Sts2.Core.Entities.Creatures;
using MyFirstCharacter.MyFirstCharacterCode.Relics;

namespace MyFirstCharacter.MyFirstCharacterCode.Powers;

/// <summary>根据当前持有的遗物（起始遗物 / 先古遗物）决定形态入场数值。</summary>
public static class CasterFormValues
{
    public static bool HasAncientRelic(Creature c) => c.Player?.GetRelic<AncientHopeStarRelic>() != null;

    public static int FormOneNp(Creature c) => HasAncientRelic(c) ? 30 : 20;

    public static int FormTwoStrength(Creature c) => HasAncientRelic(c) ? 4 : 2;

    public static int FormTwoVulnerable(Creature c) => HasAncientRelic(c) ? 1 : 2;
}

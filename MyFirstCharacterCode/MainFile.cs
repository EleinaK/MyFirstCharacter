using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MyFirstCharacter.MyFirstCharacterCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MyFirstCharacter";
    public const string ResPath = $"res://{ModId}";
    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();

        // Subscribe to NP gauge filled event - auto-generates NP card when threshold crossed
        FGOCore.NpCharge.GaugeFilled += Cards.CasterCardHelpers.CheckNPThreshold;
    }
}

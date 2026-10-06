using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        // 必须在 ModelDb.InitIds 之前跑（mod initializer 阶段 VeryEarly，早于 ExecuteEssential 里的 InitIds），
        // 因为 BaseLib 的 SavedSpireFieldPatch 是在 InitIds 的 prefix 里做排序注入的。
        FGOCore.NpFields.EnsureRegistered();
        Logger.Info("[NP] SavedSpireField 已注册: "
            + string.Join(", ", FGOCore.NpFields.RegisteredNames()));

        Harmony harmony = new(ModId);
        List<MethodInfo> applied = PatchAssembly(harmony, typeof(MainFile).Assembly);
        Logger.Info($"[ACL] 补丁已挂 {applied.Count} 个方法: "
            + string.Join(", ", applied.Select(m => $"{m.DeclaringType?.Name}.{m.Name}")));

        // Subscribe to NP gauge filled event - auto-generates NP card when threshold crossed
        FGOCore.NpCharge.NpChanged += Cards.CasterCardHelpers.CheckNPThreshold;
    }

    /// <summary>
    /// 自己扫程序集挂补丁，**不要**用 Harmony.PatchAll(Assembly)。
    ///
    /// 本仓库引用的是游戏自带的 `data_sts2_windows_x86_64/0Harmony.dll`，它里面 PatchAll(Assembly)
    /// 的实现是：
    ///     GetTypesFromAssembly(asm).DoIf(t => t.HasHarmonyAttribute(), t => CreateClassProcessor(t).Patch());
    /// 而 `HasHarmonyAttribute()` == `HarmonyMethodExtensions.GetFromType(type).Count > 0`，
    /// **只读「类级」[HarmonyPatch]**（GetFromType 走 AccessTools.GetCustomAttributes(type, ...)）。
    ///
    /// 本 mod 的补丁类（AncientCardLookPatch / CasterPowerPatches）用的都是社区最常见的「方法级」写法：
    /// [HarmonyPatch(...)] 写在方法上、配 [HarmonyPrefix/Postfix/Finalizer]，类上没有任何标注。
    /// 于是它们被那个过滤条件整个丢掉 —— PatchAll 挂 0 个、补丁全部静默失效、连一条报错都没有。
    /// （2026-10-05 实测：日志里 `PatchAll(...) 命中 0 个`，AtlasManager 从未加载 compressed_atlas。）
    ///
    /// 这里把过滤放宽成「类级有标注 **或** 任一方法有标注」，其余交给 PatchClassProcessor。
    /// 它本来就支持只有方法级标注的类：containerAttributes 由空列表 Merge 出默认值，
    /// 方法级信息在同一 Merge 里保留下来。
    /// </summary>
    private static List<MethodInfo> PatchAssembly(Harmony harmony, Assembly assembly)
    {
        List<MethodInfo> applied = [];
        foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
        {
            bool annotated = HarmonyMethodExtensions.GetFromType(type).Count > 0
                || AccessTools.GetDeclaredMethods(type).Any(m => HarmonyMethodExtensions.GetFromMethod(m).Count > 0);
            if (!annotated) continue;

            try
            {
                List<MethodInfo> patched = harmony.CreateClassProcessor(type).Patch();
                // 有标注却挂 0 个，是另一个静默失效的坑（Harmony 这时既不抛也不报）——
                // 之前排查「补丁根本没生效」花了几小时，就在这一类无声失败上。
                if (patched.Count == 0)
                    Logger.Warn($"[ACL] 补丁类 {type.FullName} 有标注但一个都没挂上（静默失效）");
                applied.AddRange(patched);
            }
            catch (Exception ex)
            {
                Logger.Error($"[ACL] 补丁类 {type.FullName} 挂载失败: {ex}");
            }
        }
        return applied;
    }
}

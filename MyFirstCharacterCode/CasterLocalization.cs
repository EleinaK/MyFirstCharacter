using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;

namespace MyFirstCharacter.MyFirstCharacterCode;

/// <summary>
/// 从 DLL 内嵌的 localization JSON 资源加载文本，供 ILocalizationProvider 使用。
/// 用于绕过 .pck（.pck 里的本地化是旧的，且本地缺少 Godot 无法重建）。
/// </summary>
public static class CasterLocalization
{
    private static readonly Dictionary<string, Dictionary<string, string>> _cache = new();

    /// <summary>返回某个模型（entry 前缀）的本地化条目，形如 (suffix, text)。</summary>
    public static List<(string, string)> ForEntry(string entry, string table)
    {
        var json = Load(table);
        var result = new List<(string, string)>();
        foreach (var kvp in json)
            if (kvp.Key.StartsWith(entry + ".", StringComparison.Ordinal))
                result.Add((kvp.Key.Substring(entry.Length + 1), kvp.Value));
        return result;
    }

    private static Dictionary<string, string> Load(string table)
    {
        if (_cache.TryGetValue(table, out var cached)) return cached;
        var result = new Dictionary<string, string>();
        foreach (var lang in new[] { CurrentLang(), "eng" }) {
            foreach (var kvp in LoadResource($"localization.{lang}.{table}.json"))
                if (!result.ContainsKey(kvp.Key))
                    result[kvp.Key] = kvp.Value;
        }
        _cache[table] = result;
        return result;
    }

    private static string CurrentLang()
    {
        try { return (LocManager.Instance?.Language ?? "zhs").StartsWith("zh") ? "zhs" : "eng"; }
        catch { return "zhs"; }
    }

    private static Dictionary<string, string> LoadResource(string suffix)
    {
        var assembly = typeof(CasterLocalization).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (name == null) return new();
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream == null) return new();
        using var reader = new StreamReader(stream);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd()) ?? new();
    }
}

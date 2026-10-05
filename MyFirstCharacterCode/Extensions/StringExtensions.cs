using Godot;

namespace MyFirstCharacter.MyFirstCharacterCode.Extensions;

//Mostly utilities to get asset paths.
//
// NOTE: these build Godot `res://` paths, so they must use FORWARD slashes.
// System.IO.Path.Join uses the platform separator (backslash on Windows), which
// produces e.g. "res://MyFirstCharacter\images\card_portraits\alone.png" — not a
// valid Godot resource path, so ResourceLoader.Exists() always returned false and
// every lookup silently fell back to the placeholder.
public static class StringExtensions
{
    private static string Res(params string[] parts)
    {
        return string.Join("/", parts);
    }

    public static string ImagePath(this string path)
    {
        return Res(MainFile.ResPath, "images", path);
    }

    public static string CardImagePath(this string path)
    {
        var res = Res(MainFile.ResPath, "images", "card_portraits", path);
        if (ResourceLoader.Exists(res)) return res;

        MainFile.Logger.Info("Could not find card image path: " + res);
        return Res(MainFile.ResPath, "images", "card_portraits", "card.png");
    }

    public static string BigCardImagePath(this string path)
    {
        var res = Res(MainFile.ResPath, "images", "card_portraits", "big", path);
        if (ResourceLoader.Exists(res)) return res;

        MainFile.Logger.Info("Could not find big card image path: " + res);
        return Res(MainFile.ResPath, "images", "card_portraits", "big", "card.png");
    }

    public static string PowerImagePath(this string path)
    {
        var res = Res(MainFile.ResPath, "images", "powers", path);
        if (ResourceLoader.Exists(res)) return res;

        MainFile.Logger.Info("Could not find power image path: " + res);
        return Res(MainFile.ResPath, "images", "powers", "power.png");
    }

    public static string BigPowerImagePath(this string path)
    {
        var res = Res(MainFile.ResPath, "images", "powers", "big", path);
        if (ResourceLoader.Exists(res)) return res;

        MainFile.Logger.Info("Could not find big power image path: " + res);
        return Res(MainFile.ResPath, "images", "powers", "big", "power.png");
    }

    public static string RelicImagePath(this string path)
    {
        var res = Res(MainFile.ResPath, "images", "relics", path);
        if (ResourceLoader.Exists(res)) return res;

        MainFile.Logger.Info("Could not find relic image path: " + res);
        return Res(MainFile.ResPath, "images", "relics", "relic.png");
    }

    public static string BigRelicImagePath(this string path)
    {
        var res = Res(MainFile.ResPath, "images", "relics", "big", path);
        if (ResourceLoader.Exists(res)) return res;

        MainFile.Logger.Info("Could not find big relic image path: " + res);
        return Res(MainFile.ResPath, "images", "relics", "big", "relic.png");
    }

    public static string CharacterUiPath(this string path)
    {
        return Res(MainFile.ResPath, "images", "charui", path);
    }
}

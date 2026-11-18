using System.Globalization;
using System.IO;

namespace ImeHalfWidth;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public bool HalfWidth { get; set; } = true;
    public bool EnglishPunctuation { get; set; } = true;

    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImeHalfWidth", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            var json = File.ReadAllText(FilePath);
            return new AppSettings
            {
                Enabled = ReadBool(json, "Enabled", true),
                HalfWidth = ReadBool(json, "HalfWidth", true),
                EnglishPunctuation = ReadBool(json, "EnglishPunctuation", true)
            };
        }
        catch
        {
            // keep defaults
        }

        return new AppSettings();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath,
            "{\r\n" +
            $"  \"Enabled\": {Bool(Enabled)},\r\n" +
            $"  \"HalfWidth\": {Bool(HalfWidth)},\r\n" +
            $"  \"EnglishPunctuation\": {Bool(EnglishPunctuation)}\r\n" +
            "}\r\n");
    }

    private static string Bool(bool value) => value ? "true" : "false";

    private static bool ReadBool(string json, string name, bool fallback)
    {
        var token = "\"" + name + "\"";
        var index = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return fallback;

        var colon = json.IndexOf(':', index + token.Length);
        if (colon < 0)
            return fallback;

        var rest = json.Substring(colon + 1).TrimStart();
        if (rest.StartsWith("true", true, CultureInfo.InvariantCulture))
            return true;
        if (rest.StartsWith("false", true, CultureInfo.InvariantCulture))
            return false;
        return fallback;
    }
}

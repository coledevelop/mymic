using System;
using System.IO;
using System.Text.Json;

namespace MyMic.Settings;

public sealed class AppSettings
{
    public HotkeyBinding? ToggleMuteHotkey { get; set; }

    private static string ConfigDir
    {
        get
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? "";
            return Path.Combine(home, "Library", "Application Support", "MyMic");
        }
    }

    private static string ConfigPath => Path.Combine(ConfigDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
    };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new AppSettings();
            var text = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppSettings>(text, JsonOpts) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var text = JsonSerializer.Serialize(this, JsonOpts);
            File.WriteAllText(ConfigPath, text);
        }
        catch
        {
            // Persistence is best-effort; failing here shouldn't crash the app.
        }
    }
}

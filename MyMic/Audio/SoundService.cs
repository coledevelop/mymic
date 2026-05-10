using System.Diagnostics;

namespace MyMic.Audio;

public static class SoundService
{
    // macOS built-in alert sounds — short, distinct, and respect the user's
    // alert volume in System Settings → Sound.
    private const string MuteSound = "/System/Library/Sounds/Funk.aiff";
    private const string UnmuteSound = "/System/Library/Sounds/Glass.aiff";

    public static void PlayToggle(bool muted)
    {
        var path = muted ? MuteSound : UnmuteSound;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/afplay",
                Arguments = $"\"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            // Fire-and-forget — afplay runs to completion in its own process.
            Process.Start(psi);
        }
        catch
        {
            // Best-effort: silent if afplay isn't available for some reason.
        }
    }
}

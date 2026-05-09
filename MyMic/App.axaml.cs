using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using MyMic.Audio;

namespace MyMic;

public partial class App : Application
{
    private MicMuteService? _mic;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _toggleItem;
    private WindowIcon? _iconOn;
    private WindowIcon? _iconMuted;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        macOS.MacOSInterop.HideFromDock();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        _mic = new MicMuteService();
        _mic.MuteChanged += (_, muted) => Dispatcher.UIThread.Post(() => UpdateTrayState(muted));

        var icons = TrayIcon.GetIcons(this);
        _trayIcon = icons is { Count: > 0 } ? icons[0] : null;
        _toggleItem = _trayIcon?.Menu?.Items.Count > 0
            ? _trayIcon.Menu.Items[0] as NativeMenuItem
            : null;

        _iconOn = LoadIcon("avares://MyMic/Assets/mic.png");
        _iconMuted = LoadIcon("avares://MyMic/Assets/mic-muted.png");

        UpdateTrayState(_mic.IsMuted);

        base.OnFrameworkInitializationCompleted();
    }

    private static WindowIcon? LoadIcon(string uri)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            return new WindowIcon(stream);
        }
        catch
        {
            return null;
        }
    }

    private void UpdateTrayState(bool muted)
    {
        if (_trayIcon is not null)
        {
            var icon = muted ? _iconMuted : _iconOn;
            if (icon is not null) _trayIcon.Icon = icon;
            _trayIcon.ToolTipText = muted ? "MyMic — mic is muted" : "MyMic — mic is on";
        }
        if (_toggleItem is not null)
        {
            _toggleItem.Header = muted ? "Unmute microphone" : "Mute microphone";
        }
    }

    private void OnToggleClicked(object? sender, EventArgs e) => _mic?.Toggle();

    private void OnTrayClicked(object? sender, EventArgs e) => _mic?.Toggle();

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}

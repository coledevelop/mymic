using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MyMic.Audio;
using MyMic.macOS;
using MyMic.Settings;

namespace MyMic;

public partial class App : Application
{
    private const int MuteMenuIndex = 0;

    private MicMuteService? _mic;
    private MacTrayIcon? _tray;
    private GlobalHotkey? _hotkey;
    private MainWindow? _window;

    public MicMuteService? Mic => _mic;
    public AppSettings Settings { get; private set; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        MacOSInterop.HideFromDock();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        Settings = AppSettings.Load();

        _mic = new MicMuteService();
        _mic.MuteChanged += (_, muted) => Dispatcher.UIThread.Post(() => UpdateTrayState(muted));

        _tray = new MacTrayIcon();
        _tray.OnOptionClick = () => _mic?.Toggle();

        var iconPath = ResolveAsset("mic.png");
        if (iconPath is not null) _tray.SetImage(iconPath);
        _tray.SetTooltip("MyMic — mic is on");

        _tray.SetMenu(new[]
        {
            new TrayMenuItem { Title = "Mute microphone", Click = () => _mic?.Toggle() },
            TrayMenuItem.Separator,
            new TrayMenuItem { Title = "Open MyMic", Click = OpenWindow },
            TrayMenuItem.Separator,
            new TrayMenuItem { Title = "Quit MyMic", Click = QuitApp },
        });

        _hotkey = new GlobalHotkey { Pressed = () => Dispatcher.UIThread.Post(() => _mic?.Toggle()) };
        ApplyHotkeyFromSettings();

        UpdateTrayState(_mic.IsMuted);

        base.OnFrameworkInitializationCompleted();
    }

    public void ApplySettings(AppSettings settings)
    {
        Settings = settings;
        Settings.Save();
        ApplyHotkeyFromSettings();
    }

    private void ApplyHotkeyFromSettings()
    {
        if (_hotkey is null) return;
        var hk = Settings.ToggleMuteHotkey;
        if (hk is null)
        {
            _hotkey.Unset();
            return;
        }
        _hotkey.TrySet(hk.MacKeyCode, hk.MacModifiers);
    }

    private void UpdateTrayState(bool muted)
    {
        if (_tray is null) return;
        var asset = ResolveAsset(muted ? "mic-muted.png" : "mic.png");
        if (asset is not null) _tray.SetImage(asset);
        _tray.SetTooltip(muted ? "MyMic — mic is muted" : "MyMic — mic is on");
        _tray.UpdateMenuItemTitle(MuteMenuIndex, muted ? "Unmute microphone" : "Mute microphone");
    }

    private static string? ResolveAsset(string fileName)
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(AppContext.BaseDirectory, "Assets", fileName),
            Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty, "Resources", fileName),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private void OpenWindow()
    {
        if (_window is null)
        {
            _window = new MainWindow();
            _window.Closed += OnWindowClosed;
        }
        MacOSInterop.ShowInDock();
        _window.Show();
        _window.Activate();
        MacOSInterop.ActivateApp();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_window is not null)
        {
            _window.Closed -= OnWindowClosed;
            _window = null;
        }
        MacOSInterop.HideFromDock();
    }

    private void QuitApp()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}

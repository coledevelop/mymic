using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace MyMic;

public partial class MainWindow : AppWindow
{
    private SettingsWindow? _settings;
    private AboutWindow? _about;
    private Bitmap? _micOnBitmap;
    private Bitmap? _micMutedBitmap;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _micOnBitmap = LoadBitmap("avares://MyMic/Assets/mic.png");
        _micMutedBitmap = LoadBitmap("avares://MyMic/Assets/mic-muted.png");

        var mic = ((App)Application.Current!).Mic;
        if (mic is not null)
        {
            UpdateMicImage(mic.IsMuted);
            mic.MuteChanged += OnMicMuteChanged;
            Closed += (_, _) => mic.MuteChanged -= OnMicMuteChanged;
        }

        KeyDown += OnKeyDown;
    }

    private static Bitmap? LoadBitmap(string uri)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private void OnMicMuteChanged(object? sender, bool muted)
        => Dispatcher.UIThread.Post(() => UpdateMicImage(muted));

    private void UpdateMicImage(bool muted)
    {
        if (this.FindControl<Image>("MicImage") is { } img)
        {
            img.Source = muted ? _micMutedBitmap : _micOnBitmap;
        }
    }

    private void OnMicClick(object? sender, RoutedEventArgs e)
        => ((App)Application.Current!).Mic?.Toggle();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.OemComma && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            OpenSettings();
            e.Handled = true;
        }
    }

    private void OnOpenSettings(object? sender, RoutedEventArgs e) => OpenSettings();

    private void OnOpenAbout(object? sender, RoutedEventArgs e)
    {
        _settings?.Close();

        if (_about is null)
        {
            _about = new AboutWindow();
            _about.Closed += (_, _) => _about = null;
        }
        _about.Show(this);
        _about.Activate();
    }

    private void OpenSettings()
    {
        _about?.Close();

        if (_settings is null)
        {
            _settings = new SettingsWindow();
            _settings.Closed += (_, _) => _settings = null;
        }
        _settings.Show(this);
        _settings.Activate();
    }
}

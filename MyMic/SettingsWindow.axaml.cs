using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MyMic.Settings;

namespace MyMic;

public partial class SettingsWindow : SecondaryWindow
{
    private bool _recording;
    private AppSettings _settings = null!;
    private Button? _hotkeyButton;

    public SettingsWindow()
    {
        AvaloniaXamlLoader.Load(this);

        if (Application.Current is App app)
        {
            _settings = app.Settings;
        }
        else
        {
            _settings = AppSettings.Load();
        }

        _hotkeyButton = this.FindControl<Button>("HotkeyButton");
        if (_hotkeyButton is not null)
        {
            // Tunnel so we see the key before any framework handling (Tab, etc).
            _hotkeyButton.AddHandler(KeyDownEvent, OnHotkeyKeyDown, RoutingStrategies.Tunnel);
            _hotkeyButton.LostFocus += (_, _) => StopRecording(commit: false);
        }

        UpdateHotkeyDisplay();
    }

    private void OnHotkeyButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_recording) return;
        StartRecording();
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _settings.ToggleMuteHotkey = null;
        if (Application.Current is App app) app.ApplySettings(_settings);
        else _settings.Save();
        UpdateHotkeyDisplay();
    }

    private void StartRecording()
    {
        _recording = true;
        if (_hotkeyButton is not null)
        {
            _hotkeyButton.Content = "Press a key combo… (Esc to cancel)";
            _hotkeyButton.Classes.Add("recording");
            _hotkeyButton.Focus();
        }
    }

    private void StopRecording(bool commit)
    {
        _recording = false;
        if (_hotkeyButton is not null)
        {
            _hotkeyButton.Classes.Remove("recording");
        }
        UpdateHotkeyDisplay();
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_recording) return;
        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            StopRecording(commit: false);
            return;
        }

        if (KeyMapping.IsModifierOnly(e.Key)) return;

        if (!KeyMapping.TryGetMacKeyCode(e.Key, out var keyCode))
        {
            // Unsupported key — just keep waiting for a different one.
            return;
        }

        var modifiers = KeyMapping.AvModifiersToMac(e.KeyModifiers);
        var display = KeyMapping.FormatDisplay(e.Key, e.KeyModifiers);

        _settings.ToggleMuteHotkey = new HotkeyBinding
        {
            MacKeyCode = keyCode,
            MacModifiers = modifiers,
            Display = display,
        };

        if (Application.Current is App app) app.ApplySettings(_settings);
        else _settings.Save();

        StopRecording(commit: true);
    }

    private void UpdateHotkeyDisplay()
    {
        if (_hotkeyButton is null) return;
        var hk = _settings.ToggleMuteHotkey;
        _hotkeyButton.Content = hk is null ? "Click to set" : hk.Display;
    }
}

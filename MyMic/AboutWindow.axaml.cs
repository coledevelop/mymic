using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MyMic.Updates;

namespace MyMic;

public partial class AboutWindow : SecondaryWindow
{
    private UpdaterService? _updater;
    private Button? _updateButton;
    private TextBlock? _updateStatus;
    private TextBlock? _updateCheckmark;

    public AboutWindow()
    {
        AvaloniaXamlLoader.Load(this);

        if (this.FindControl<TextBlock>("VersionLabel") is { } label)
        {
            var v = typeof(AboutWindow).Assembly.GetName().Version;
            label.Text = v is null ? "Version unknown" : $"Version {v.ToString(3)}";
        }

        _updateButton = this.FindControl<Button>("UpdateButton");
        _updateStatus = this.FindControl<TextBlock>("UpdateStatus");
        _updateCheckmark = this.FindControl<TextBlock>("UpdateCheckmark");

        if (Application.Current is App app)
        {
            _updater = app.Updater;
            if (_updater is not null)
            {
                _updater.Changed += OnUpdaterChanged;
                Closed += (_, _) => _updater.Changed -= OnUpdaterChanged;
                Render();
            }
        }
    }

    private void OnUpdaterChanged(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(Render);

    private void Render()
    {
        if (_updater is null) return;
        if (_updateButton is not null)
        {
            _updateButton.Content = _updater.ButtonText;
            _updateButton.IsEnabled = _updater.ButtonEnabled;
        }
        if (_updateStatus is not null)
        {
            _updateStatus.Text = _updater.StatusText;
        }
        if (_updateCheckmark is not null)
        {
            _updateCheckmark.IsVisible = _updater.State == UpdaterState.UpToDate;
        }
    }

    private async void OnUpdateClick(object? sender, RoutedEventArgs e)
    {
        if (_updater is null) return;
        await _updater.TriggerAsync();
    }
}

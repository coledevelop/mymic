using Avalonia.Markup.Xaml;

namespace MyMic;

public partial class SettingsWindow : SecondaryWindow
{
    public SettingsWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

using Avalonia.Markup.Xaml;

namespace MyMic;

public partial class AboutWindow : SecondaryWindow
{
    public AboutWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

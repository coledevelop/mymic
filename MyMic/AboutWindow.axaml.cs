using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MyMic;

public partial class AboutWindow : SecondaryWindow
{
    public AboutWindow()
    {
        AvaloniaXamlLoader.Load(this);

        if (this.FindControl<TextBlock>("VersionLabel") is { } label)
        {
            var v = typeof(AboutWindow).Assembly.GetName().Version;
            label.Text = v is null ? "Version unknown" : $"Version {v.ToString(3)}";
        }
    }
}

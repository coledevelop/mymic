using Avalonia.Controls;
using Avalonia.Input;

namespace MyMic;

public class AppWindow : Window
{
    public AppWindow()
    {
        KeyDown += OnAppWindowKeyDown;
    }

    private void OnAppWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.W && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            Close();
            e.Handled = true;
        }
    }
}

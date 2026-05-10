namespace MyMic;

public class SecondaryWindow : AppWindow
{
    public SecondaryWindow()
    {
        Deactivated += (_, _) => Close();
    }
}

using System.Windows;

namespace ImeHalfWidth;

public enum CloseChoice
{
    Cancel,
    Tray,
    Exit
}

public partial class ClosePromptWindow : Window
{
    public CloseChoice Choice { get; private set; } = CloseChoice.Cancel;

    public ClosePromptWindow()
    {
        InitializeComponent();
    }

    private void OnTray(object sender, RoutedEventArgs e)
    {
        Choice = CloseChoice.Tray;
        DialogResult = true;
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        Choice = CloseChoice.Exit;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Choice = CloseChoice.Cancel;
        DialogResult = false;
    }
}

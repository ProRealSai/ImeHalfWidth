using System.Windows;
using System.Windows.Threading;

namespace ImeHalfWidth;

public partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show(e.Exception.Message, "半角输入锁定", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}

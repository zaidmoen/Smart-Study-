using System.Windows;

namespace SmartStudy;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "حدث خطأ غير متوقع. تم منع إغلاق التطبيق حتى لا تفقد بياناتك.\n\n" + args.Exception.Message,
                "Smart Study",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}

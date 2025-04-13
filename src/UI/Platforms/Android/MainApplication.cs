using Android.App;
using Android.Runtime;

namespace Binnaculum;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp()
    {
        var app = MauiProgram.CreateMauiApp();
        var service = app.Services.GetService(typeof(Microsoft.Maui.IApplication));
        if (service is Binnaculum.App binnaculum)
        {
            Binnaculum.App.IniLogStartupTime();
        }

        return app;
    }
}

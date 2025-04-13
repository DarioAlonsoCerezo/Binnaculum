using System.Diagnostics;

namespace Binnaculum;

public partial class App : Application
{
    private static readonly Stopwatch StartupStopwatch = new Stopwatch();
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        //Restore the theme and language settings
        Current!.UserAppTheme = (AppTheme)Preferences.Get("AppTheme", (int)AppTheme.Unspecified);
        LocalizationResourceManager.Instance.SetCulture(new CultureInfo(Preferences.Get("Language", "en")));
        
        return new Window(new NavigationPage(new AppShell()));
    }

    protected override void OnStart()
    {
        base.OnStart();
        
    }

    protected override void OnResume()
    {
        base.OnResume();
    }

    public static void IniLogStartupTime()
    {
        // Start the stopwatch when the app is initialized
        StartupStopwatch.Start();
    }

    public static void LogStartupTime()
    {
        // Stop the stopwatch and log the elapsed time
        StartupStopwatch.Stop();
        Debug.WriteLine($"App startup time: {StartupStopwatch.ElapsedMilliseconds} ms");
    }
}
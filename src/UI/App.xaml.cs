namespace Binnaculum;

public partial class App : Application
{
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
}
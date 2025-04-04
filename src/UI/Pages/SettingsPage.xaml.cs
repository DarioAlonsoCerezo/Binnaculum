namespace Binnaculum.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly CompositeDisposable Disposables;
    public SettingsPage()
	{
		InitializeComponent();

        Disposables = new CompositeDisposable();

        this.Events().Appearing
            .Do(_ => SetupThemeRadioButtons())
            .Do(_ => SetupLanguageRadioButtons())
            .Subscribe()
            .DisposeWith(Disposables);

        SetupEvents();
    }

    private void SetupEvents()
    {
        LightRadioButton.Events().CheckedChanged
            .Where(x => x.Value)
            .Do(_ => SetupTheme(AppTheme.Light))
            .Subscribe()
            .DisposeWith(Disposables);

        DarkRadioButton.Events().CheckedChanged
            .Where(x => x.Value)
            .Do(_ => SetupTheme(AppTheme.Dark))
            .Subscribe()
            .DisposeWith(Disposables);

        DeviceRadioButton.Events().CheckedChanged
            .Where(x => x.Value)
            .Do(_ => SetupTheme(AppTheme.Unspecified))
            .Subscribe()
            .DisposeWith(Disposables);

        LanguageEnglishRadioButton.Events().CheckedChanged
            .Where(x => x.Value)
            .Do(_ => SetupLanguage("en"))
            .Subscribe()
            .DisposeWith(Disposables);

        LanguageSpanishRadioButton.Events().CheckedChanged
            .Where(x => x.Value)
            .Do(_ => SetupLanguage("es"))
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void SetupTheme(AppTheme theme)
    {
        if (Application.Current!.UserAppTheme != theme)
        {
            Application.Current.UserAppTheme = theme;
            Preferences.Set("AppTheme", (int)theme);
        }
        SetupThemeRadioButtons();
    }

    private void SetupThemeRadioButtons()
    {
        LightRadioButton.IsChecked = Application.Current!.UserAppTheme == AppTheme.Light;
        DarkRadioButton.IsChecked = Application.Current.UserAppTheme == AppTheme.Dark;
        DeviceRadioButton.IsChecked = Application.Current.UserAppTheme == AppTheme.Unspecified;
    }

    private void SetupLanguage(string code)
    {
        if (!AppResources.Culture.TwoLetterISOLanguageName.Equals(code))
        {
            LocalizationResourceManager.Instance.SetCulture(new CultureInfo(code));
            Preferences.Set("Language", code);
        }
        SetupLanguageRadioButtons();
    }

    private void SetupLanguageRadioButtons()
    {
        LanguageEnglishRadioButton.IsChecked = AppResources.Culture.TwoLetterISOLanguageName == "en";
        LanguageSpanishRadioButton.IsChecked = AppResources.Culture.TwoLetterISOLanguageName == "es";
    }
}
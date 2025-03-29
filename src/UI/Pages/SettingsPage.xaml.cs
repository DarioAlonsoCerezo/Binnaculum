

using Binnaculum.Resources.Languages;

namespace Binnaculum.Pages;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        SetupThemeRadioButtons();
        SetupLanguageRadioButtons();
    }

    

    private void LightRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
            SetupTheme(AppTheme.Light);
    }

    private void DarkRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if(e.Value)
            SetupTheme(AppTheme.Dark);
    }

    private void DeviceRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
            SetupTheme(AppTheme.Unspecified);
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

    private void LanguageEnglishRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if(e.Value)
            SetupLanguage("en");
    }

    private void LanguageSpanishRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
            SetupLanguage("es");
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
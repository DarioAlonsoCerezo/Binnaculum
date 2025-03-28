
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
}
using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly CompositeDisposable Disposables = [];
    public SettingsPage()
	{
		InitializeComponent();

        SetupEvents();
    }

    private void SetupEvents()
    {
        Core.UI.SavedPrefereces.UserPreferences
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(SetupPreferencesCollection)
            .DisposeWith(Disposables);

        AllowCreateAccountsSwitch.Events().Toggled
            .Do(Core.UI.SavedPrefereces.ChangeAllowCreateAccount)
            .Subscribe()
            .DisposeWith(Disposables);

        LightRadioButton.Events().Clicked
            .Do(_ => SetupTheme(AppTheme.Light))
            .Subscribe()
            .DisposeWith(Disposables);

        DarkRadioButton.Events().Clicked
            .Do(_ => SetupTheme(AppTheme.Dark))
            .Subscribe()
            .DisposeWith(Disposables);

        DeviceRadioButton.Events().Clicked
            .Do(_ => SetupTheme(AppTheme.Unspecified))
            .Subscribe()
            .DisposeWith(Disposables);

        LanguageEnglishRadioButton.Events().Clicked
            .Do(_ => SetupLanguage("en"))
            .Subscribe()
            .DisposeWith(Disposables);

        LanguageSpanishRadioButton.Events().Clicked
            .Do(_ => SetupLanguage("es"))
            .Subscribe()
            .DisposeWith(Disposables);

        DefaultCurrencyGesture.Events().Tapped
        .SelectMany(_ => Observable.FromAsync(async () =>
        {
            var popup = new CurrencySelectorPopup();
            var appMainpage = Application.Current!.Windows[0].Page!;
            if (appMainpage is NavigationPage navigator)
            {
                var result = await navigator.ShowPopupAsync(popup);
                if (result is Models.Currency currency)
                {
                    DefaultCurrency.Text = currency.Code;
                    Core.UI.SavedPrefereces.ChangeCurrency(currency.Code);
                }
            }
            return Unit.Default; // Return Unit.Default as a "void" equivalent
        }))
        .Subscribe()
        .DisposeWith(Disposables);
    }

    private void SetupTheme(AppTheme theme)
    {
        if (Application.Current!.UserAppTheme != theme)
        {
            Application.Current.UserAppTheme = theme;
            Core.UI.SavedPrefereces.ChangeAppTheme(theme);
        }
    }

    private void SetupLanguage(string code)
    {
        AppResources.Culture.TwoLetterISOLanguageName.Equals(code);
        LocalizationResourceManager.Instance.SetCulture(new CultureInfo(code));
        Core.UI.SavedPrefereces.ChangeLanguage(code);
    }

    private void SetupPreferencesCollection(Core.UI.SavedPrefereces.PreferencesCollection collection)
    {
        DefaultCurrency.Text = collection.Currency;
        AllowCreateAccountsSwitch.IsOn = collection.AllowCreateAccount;

        LanguageEnglishRadioButton.IsChecked = collection.Language.Equals("en");
        LanguageSpanishRadioButton.IsChecked = collection.Language.Equals("es");

        LightRadioButton.IsChecked = collection.Theme == AppTheme.Light;
        DarkRadioButton.IsChecked = collection.Theme == AppTheme.Dark;
        DeviceRadioButton.IsChecked = collection.Theme == AppTheme.Unspecified;
    }
}
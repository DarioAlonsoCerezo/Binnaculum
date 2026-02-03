using Binnaculum.Core;
using Binnaculum.Popups;
using System.Diagnostics;

namespace Binnaculum.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly CompositeDisposable Disposables = [];
    public SettingsPage()
	{
		InitializeComponent();

        AppVersion.Text = $"v{AppInfo.VersionString}({AppInfo.BuildString})";

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

        GroupOptionTrades.Events().Toggled
            .Do(Core.UI.SavedPrefereces.ChangeGroupOption)
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
            var popupResult = await new CurrencySelectorPopup().ShowAndWait();
            if (popupResult.Result is Models.Currency currency)
            {
                DefaultCurrency.Text = currency.Code;
                Core.UI.SavedPrefereces.ChangeCurrency(currency.Code);
            }
            return Unit.Default; // Return Unit.Default as a "void" equivalent
        }))
        .Subscribe()
        .DisposeWith(Disposables);

        DefaultTickerGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var popupResult = await new TickerSelectorPopup().ShowAndWait();
                if (popupResult.Result is Models.Ticker ticker)
                {
                    Core.UI.SavedPrefereces.ChangeDefaultTicker(ticker.Symbol);
                }
                return Unit.Default;
            }))
            .Subscribe()
            .DisposeWith(Disposables);

        DeleteAllDataButton.Events().Clicked
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                await HandleDeleteAllData();
                return Unit.Default;
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
        DefaultTicker.Text = collection.Ticker;
        AllowCreateAccountsSwitch.IsOn = collection.AllowCreateAccount;
        GroupOptionTrades.IsOn = collection.GroupOptions;

        LanguageEnglishRadioButton.IsChecked = collection.Language.Equals("en");
        LanguageSpanishRadioButton.IsChecked = collection.Language.Equals("es");

        LightRadioButton.IsChecked = collection.Theme == AppTheme.Light;
        DarkRadioButton.IsChecked = collection.Theme == AppTheme.Dark;
        DeviceRadioButton.IsChecked = collection.Theme == AppTheme.Unspecified;
    }

    private async void IssueMarkdownView_OnHyperLinkClicked(object sender, Indiko.Maui.Controls.Markdown.LinkEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Url))
        {
            try
            {
                // Get the current culture to determine if we should add language parameter
                var currentCulture = CultureInfo.CurrentCulture;
                var url = e.Url;

                // If Spanish is selected, add a query parameter to indicate this
                if (currentCulture.TwoLetterISOLanguageName.Equals("es", StringComparison.OrdinalIgnoreCase))
                {
                    // Add language parameter to the GitHub URL
                    if (url.Contains("?"))
                        url += "&lang=es";
                    else
                        url += "?lang=es";
                }

                // Open the URL in the default browser
                await Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening URL: {ex.Message}");
            }
        }
    }

    private async Task HandleDeleteAllData()
    {
        try
        {
            // First confirmation
            var firstConfirm = await new ConfirmDeleteAllDataPopup().ShowAndWait();
            if (firstConfirm.Result is not bool firstResult || !firstResult)
            {
                // User cancelled
                return;
            }

            // Second confirmation
            var secondConfirm = await new ConfirmDeleteAllDataPopup().ShowAndWait();
            if (secondConfirm.Result is not bool secondResult || !secondResult)
            {
                // User cancelled
                return;
            }

            // Both confirmations passed - proceed with deletion
            await Core.Database.DataResetExtensions.Do.deleteAllOperationalData();

            // Refresh app state by reloading all data
            await Core.Storage.DataLoader.loadBasicData();
            await Core.Storage.DataLoader.getOrRefreshAllAccounts();
            
            // Refresh reactive managers
            Core.UI.ReactiveSnapshotManager.refresh();

            // Show success message
            await DisplayAlert(
                AppResources.Global_Button_Ok,
                "All operational data has been deleted successfully.",
                AppResources.Global_Button_Ok);
        }
        catch (Exception ex)
        {
            // Show error message
            await DisplayAlert(
                "Error",
                $"Failed to delete data: {ex.Message}",
                AppResources.Global_Button_Ok);
        }
    }
}
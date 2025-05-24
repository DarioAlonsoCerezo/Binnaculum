namespace Binnaculum.Core.UI

open Microsoft.Maui.ApplicationModel
open Microsoft.Maui.Storage
open System.Reactive.Subjects

module SavedPrefereces =
    
    let private parseTheme theme =
        match theme with
        | AppTheme.Unspecified -> 0
        | AppTheme.Light -> 1
        | AppTheme.Dark -> 2
        | _ -> 0

    let private themeIntToEnum (theme: int) =
        match theme with
        | 0 -> AppTheme.Unspecified
        | 1 -> AppTheme.Light
        | 2 -> AppTheme.Dark
        | _ -> AppTheme.Unspecified
    
    [<Literal>]
    let private ThemeKey = "AppTheme"

    [<Literal>]
    let private LanguageKey = "Language"

    [<Literal>]
    let private CurrencyKey = "Currency"

    [<Literal>]
    let private TickerKey = "Ticker"

    [<Literal>]
    let private AllowCreateAccountKey = "AllowCreateAccount"

    [<Literal>]
    let private DefaultLanguage = "en"

    [<Literal>]
    let private DefaultCurrency = "USD"

    [<Literal>]
    let private DefaultTicker = "SPY"
    
    type PreferencesCollection = {
        Theme: AppTheme
        Language: string
        Currency: string
        AllowCreateAccount: bool
        Ticker: string
    }

    let private loadPreferences() = 
        let theme = Preferences.Get(ThemeKey, parseTheme AppTheme.Unspecified)
        let language = Preferences.Get(LanguageKey, DefaultLanguage)
        let currency = Preferences.Get(CurrencyKey, DefaultCurrency)
        let allowCreateAccount = Preferences.Get(AllowCreateAccountKey, true)
        let defaultTicker = Preferences.Get(TickerKey, DefaultTicker)
        { Theme = themeIntToEnum theme; Language = language; Currency = currency; AllowCreateAccount = allowCreateAccount; Ticker = defaultTicker }

    let UserPreferences = new BehaviorSubject<PreferencesCollection>(loadPreferences())

    let ChangeAppTheme theme =
        Preferences.Set(ThemeKey, parseTheme theme)
        UserPreferences.OnNext({ UserPreferences.Value with Theme = theme })

    let ChangeLanguage(language: string) =
        Preferences.Set(LanguageKey, language)
        UserPreferences.OnNext({ UserPreferences.Value with Language = language })

    let ChangeCurrency(currency: string) =
        Preferences.Set(CurrencyKey, currency)
        UserPreferences.OnNext({ UserPreferences.Value with Currency = currency })

    let ChangeAllowCreateAccount(allow: bool) =
        Preferences.Set(AllowCreateAccountKey, allow)
        UserPreferences.OnNext({ UserPreferences.Value with AllowCreateAccount = allow })

    let ChangeDefaultTicker(ticker: string) =
        Preferences.Set(TickerKey, ticker)
        UserPreferences.OnNext({ UserPreferences.Value with Ticker = ticker })
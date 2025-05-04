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
    let private Currency = "Currency"

    [<Literal>]
    let private AllowCreateAccount = "AllowCreateAccount"

    [<Literal>]
    let private DefaultLanguage = "en"

    [<Literal>]
    let private DefaultCurrency = "USD"
    
    type PreferencesCollection = {
        Theme: AppTheme
        Language: string
        Currency: string
        AllowCreateAccount: bool
    }

    let private loadPreferences() = 
        let theme = Preferences.Get(ThemeKey, parseTheme AppTheme.Unspecified)
        let language = Preferences.Get(LanguageKey, DefaultLanguage)
        let currency = Preferences.Get(Currency, DefaultCurrency)
        let allowCreateAccount = Preferences.Get(AllowCreateAccount, true)
        { Theme = themeIntToEnum theme; Language = language; Currency = currency; AllowCreateAccount = allowCreateAccount }

    let UserPreferences = new BehaviorSubject<PreferencesCollection>(loadPreferences())

    let ChangeAppTheme theme =
        Preferences.Set(ThemeKey, parseTheme theme)
        UserPreferences.OnNext({ UserPreferences.Value with Theme = theme })

    let ChangeLanguage(language: string) =
        Preferences.Set(LanguageKey, language)
        UserPreferences.OnNext({ UserPreferences.Value with Language = language })

    let ChangeCurrency(currency: string) =
        Preferences.Set(Currency, currency)
        UserPreferences.OnNext({ UserPreferences.Value with Currency = currency })

    let ChangeAllowCreateAccount(allow: bool) =
        Preferences.Set(AllowCreateAccount, allow)
        UserPreferences.OnNext({ UserPreferences.Value with AllowCreateAccount = allow })
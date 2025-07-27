namespace Binnaculum.Core.UI

open Microsoft.Maui.ApplicationModel
open Microsoft.Maui.Storage
open System.Reactive.Subjects
open Binnaculum.Core.Storage
open System.Threading.Tasks
open Binnaculum.Core.Keys // Added for key access

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
    
    type PreferencesCollection = {
        Theme: AppTheme
        Language: string
        Currency: string
        AllowCreateAccount: bool
        Ticker: string
        GroupOptions: bool
    }

    let private loadPreferences() = 
        let theme = Preferences.Get(ThemeKey, parseTheme AppTheme.Unspecified)
        let language = Preferences.Get(LanguageKey, DefaultLanguage)
        let currency = Preferences.Get(CurrencyKey, DefaultCurrency)
        let allowCreateAccount = Preferences.Get(AllowCreateAccountKey, true)
        let defaultTicker = Preferences.Get(TickerKey, DefaultTicker)
        let groupOptions = Preferences.Get(GroupOptionsKey, true)
        { Theme = themeIntToEnum theme; Language = language; Currency = currency; AllowCreateAccount = allowCreateAccount; Ticker = defaultTicker; GroupOptions = groupOptions }

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

    // Fire-and-forget helper function
    let private fireAndForget (taskFactory: unit -> Task) =
        // Start the work on a background thread
        Task.Run(fun () ->
            taskFactory().ContinueWith(fun t ->
                if t.IsFaulted && t.Exception <> null then
                    System.Diagnostics.Debug.WriteLine($"Task failed: {t.Exception}")
            ) |> ignore
        ) |> ignore

    let ChangeGroupOption(group: bool) =
        // Check if the group option has changed
        let currentValue = UserPreferences.Value.GroupOptions
        Preferences.Set(GroupOptionsKey, group)
        UserPreferences.OnNext({ UserPreferences.Value with GroupOptions = group })
        
        // If the value has changed, reload option trades
        if currentValue <> group then            
            fireAndForget(fun () -> DataLoader.changeOptionsGrouped())

    // TODO: Add Information button to all settings where we need to explain the user
    // what the setting does.
    // Sample: When the user change options group, what's happening after the change
    // Sample: When the user change Currency, Ticket....
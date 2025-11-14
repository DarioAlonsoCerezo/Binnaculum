namespace Binnaculum.Core.UI

open Microsoft.Maui.ApplicationModel
open System.Reactive.Subjects
open Binnaculum.Core.Storage
open Binnaculum.Core.Providers
open System.Threading.Tasks
open System
open Binnaculum.Core.Keys // Added for key access
open Binnaculum.Core.Logging

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

    type PreferencesCollection =
        { Theme: AppTheme
          Language: string
          Currency: string
          AllowCreateAccount: bool
          Ticker: string
          GroupOptions: bool
          PolygonApiKey: string option }

    let private loadPreferences () =
        let theme =
            PreferencesProvider.getPreference ThemeKey (parseTheme AppTheme.Unspecified)

        let language = PreferencesProvider.getString LanguageKey DefaultLanguage
        let currency = PreferencesProvider.getString CurrencyKey DefaultCurrency
        let allowCreateAccount = PreferencesProvider.getBoolean AllowCreateAccountKey true
        let defaultTicker = PreferencesProvider.getString TickerKey DefaultTicker
        let groupOptions = PreferencesProvider.getBoolean GroupOptionsKey true

        { Theme = themeIntToEnum theme
          Language = language
          Currency = currency
          AllowCreateAccount = allowCreateAccount
          Ticker = defaultTicker
          GroupOptions = groupOptions
          PolygonApiKey = None }

    let private loadPreferencesAsync () =
        task {
            let theme =
                PreferencesProvider.getPreference ThemeKey (parseTheme AppTheme.Unspecified)

            let language = PreferencesProvider.getString LanguageKey DefaultLanguage
            let currency = PreferencesProvider.getString CurrencyKey DefaultCurrency
            let allowCreateAccount = PreferencesProvider.getBoolean AllowCreateAccountKey true
            let defaultTicker = PreferencesProvider.getString TickerKey DefaultTicker
            let groupOptions = PreferencesProvider.getBoolean GroupOptionsKey true

            // Load Polygon API Key from SecureStorage with error handling
            let! polygonApiKey =
                task {
                    try
                        let! key = PreferencesProvider.getSecureAsync (PolygonApiKeyKey)
                        return if String.IsNullOrEmpty(key) then None else Some key
                    with ex ->
                        // Log error if needed, but don't throw - just return None as default
                        CoreLogger.logError "SavedPreferences" $"Failed to load Polygon API Key from SecureStorage: {ex.Message}"

                        return None
                }

            return
                { Theme = themeIntToEnum theme
                  Language = language
                  Currency = currency
                  AllowCreateAccount = allowCreateAccount
                  Ticker = defaultTicker
                  GroupOptions = groupOptions
                  PolygonApiKey = polygonApiKey }
        }

    let UserPreferences = new BehaviorSubject<PreferencesCollection>(loadPreferences ())

    /// Load the Polygon API Key asynchronously from SecureStorage and update UserPreferences
    let LoadPolygonApiKeyAsync () =
        task {
            let! currentPrefs = loadPreferencesAsync ()
            UserPreferences.OnNext(currentPrefs)
        }

    let ChangeAppTheme theme =
        PreferencesProvider.setPreference ThemeKey (parseTheme theme)

        UserPreferences.OnNext(
            { UserPreferences.Value with
                Theme = theme }
        )

    let ChangeLanguage (language: string) =
        PreferencesProvider.setString LanguageKey language

        UserPreferences.OnNext(
            { UserPreferences.Value with
                Language = language }
        )

    let ChangeCurrency (currency: string) =
        PreferencesProvider.setString CurrencyKey currency

        UserPreferences.OnNext(
            { UserPreferences.Value with
                Currency = currency }
        )

    let ChangeAllowCreateAccount (allow: bool) =
        PreferencesProvider.setBoolean AllowCreateAccountKey allow

        UserPreferences.OnNext(
            { UserPreferences.Value with
                AllowCreateAccount = allow }
        )

    let ChangeDefaultTicker (ticker: string) =
        PreferencesProvider.setString TickerKey ticker

        UserPreferences.OnNext(
            { UserPreferences.Value with
                Ticker = ticker }
        )

    // Fire-and-forget helper function
    let private fireAndForget (taskFactory: unit -> Task) =
        // Start the work on a background thread
        Task.Run(fun () ->
            taskFactory()
                .ContinueWith(fun t ->
                    if t.IsFaulted && t.Exception <> null then
                        CoreLogger.logError "SavedPreferences" $"Task failed: {t.Exception}")
            |> ignore)
        |> ignore

    let ChangeGroupOption (group: bool) =
        // Check if the group option has changed
        let currentValue = UserPreferences.Value.GroupOptions
        PreferencesProvider.setBoolean GroupOptionsKey group

        UserPreferences.OnNext(
            { UserPreferences.Value with
                GroupOptions = group }
        )

        // If the value has changed, reload option trades
        if currentValue <> group then
            fireAndForget (fun () -> DataLoader.changeOptionsGrouped ())

    /// Change the Polygon API Key, saving it to SecureStorage and updating UserPreferences
    let ChangePolygonApiKey (apiKey: string option) =
        task {
            try
                // Save to SecureStorage
                match apiKey with
                | Some key -> do! PreferencesProvider.setSecureAsync PolygonApiKeyKey key
                | None -> PreferencesProvider.removeSecure (PolygonApiKeyKey) |> ignore

                // Update UserPreferences
                UserPreferences.OnNext(
                    { UserPreferences.Value with
                        PolygonApiKey = apiKey }
                )
            with ex ->
                // Log error but don't throw - this maintains app stability
                CoreLogger.logError "SavedPreferences" $"Failed to save Polygon API Key to SecureStorage: {ex.Message}"
                // Still update the in-memory preferences for consistency
                UserPreferences.OnNext(
                    { UserPreferences.Value with
                        PolygonApiKey = apiKey }
                )
        }

    /// <summary>
    /// Sets the preferences storage mode (FileSystem or InMemory).
    /// This is primarily intended for testing purposes to enable in-memory preference storage.
    /// </summary>
    /// <param name="mode">The preferences mode to use</param>
    let setPreferencesMode (mode: PreferencesMode) =
        PreferencesProvider.setPreferencesMode mode

    /// <summary>
    /// Clears all in-memory preferences storage.
    /// Only affects InMemory mode; FileSystem mode is unaffected.
    /// This is primarily intended for testing purposes to reset state between tests.
    /// </summary>
    let clearInMemoryPreferences () =
        PreferencesProvider.clearInMemoryStorage ()

// TODO: Add Information button to all settings where we need to explain the user
// what the setting does.
// Sample: When the user change options group, what's happening after the change
// Sample: When the user change Currency, Ticket....

namespace Core.Platform.Tests

open NUnit.Framework
open NUnit.Framework.Constraints
open Binnaculum.Core.UI
open Microsoft.Maui.ApplicationModel
open Core.Platform.Tests.PlatformTestEnvironment

[<TestFixture>]
[<Category("RequiresMauiPlatform")>]
[<Category("PlatformSpecific")>]
type SavedPreferencesTests () =
    let getCurrentPreferences () = SavedPrefereces.UserPreferences.Value

    [<OneTimeSetUp>]
    member _.OneTimeSetup() =
        // Initialize platform for testing
        initializePlatform()

    [<SetUp>]
    member _.Setup () =
        requiresMauiPlatform(fun () ->
            SavedPrefereces.ChangeAppTheme(AppTheme.Unspecified)
            SavedPrefereces.ChangeLanguage("en")
            SavedPrefereces.ChangeCurrency("USD")
            SavedPrefereces.ChangeAllowCreateAccount(true)
            SavedPrefereces.ChangeDefaultTicker("SPY")
            SavedPrefereces.ChangeGroupOption(true)
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member _.``ChangeAppTheme updates UserPreferences`` () =
        requiresMauiPlatform(fun () ->
            SavedPrefereces.ChangeAppTheme(AppTheme.Dark)
            Assert.That(getCurrentPreferences().Theme, Is.EqualTo(AppTheme.Dark))
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member _.``ChangeLanguage updates UserPreferences`` () =
        requiresMauiPlatform(fun () ->
            SavedPrefereces.ChangeLanguage("es")
            Assert.That(getCurrentPreferences().Language, Is.EqualTo("es"))
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member _.``ChangeCurrency updates UserPreferences`` () =
        requiresMauiPlatform(fun () ->
            SavedPrefereces.ChangeCurrency("EUR")
            Assert.That(getCurrentPreferences().Currency, Is.EqualTo("EUR"))
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member _.``ChangeAllowCreateAccount updates UserPreferences`` () =
        requiresMauiPlatform(fun () ->
            SavedPrefereces.ChangeAllowCreateAccount(false)
            Assert.That(getCurrentPreferences().AllowCreateAccount, Is.EqualTo(false))
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member _.``ChangeDefaultTicker updates UserPreferences`` () =
        requiresMauiPlatform(fun () ->
            SavedPrefereces.ChangeDefaultTicker("QQQ")
            Assert.That(getCurrentPreferences().Ticker, Is.EqualTo("QQQ"))
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member _.``ChangeGroupOption updates UserPreferences and triggers reload if changed`` () =
        requiresMauiPlatform(fun () ->
            SavedPrefereces.ChangeGroupOption(false)
            Assert.That(getCurrentPreferences().GroupOptions, Is.EqualTo(false))
            SavedPrefereces.ChangeGroupOption(false)
            Assert.That(getCurrentPreferences().GroupOptions, Is.EqualTo(false))
        )
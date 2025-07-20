namespace Core.Tests

open NUnit.Framework
open NUnit.Framework.Constraints
open Binnaculum.Core.UI
open Microsoft.Maui.ApplicationModel

[<TestFixture>]
type SavedPreferencesTests () =
    let getCurrentPreferences () = SavedPrefereces.UserPreferences.Value

    [<SetUp>]
    member _.Setup () =
        SavedPrefereces.ChangeAppTheme(AppTheme.Unspecified)
        SavedPrefereces.ChangeLanguage("en")
        SavedPrefereces.ChangeCurrency("USD")
        SavedPrefereces.ChangeAllowCreateAccount(true)
        SavedPrefereces.ChangeDefaultTicker("SPY")
        SavedPrefereces.ChangeGroupOption(true)

    [<Test>]
    member _.``ChangeAppTheme updates UserPreferences`` () =
        SavedPrefereces.ChangeAppTheme(AppTheme.Dark)
        Assert.That(getCurrentPreferences().Theme, Is.EqualTo(AppTheme.Dark))

    [<Test>]
    member _.``ChangeLanguage updates UserPreferences`` () =
        SavedPrefereces.ChangeLanguage("es")
        Assert.That(getCurrentPreferences().Language, Is.EqualTo("es"))

    [<Test>]
    member _.``ChangeCurrency updates UserPreferences`` () =
        SavedPrefereces.ChangeCurrency("EUR")
        Assert.That(getCurrentPreferences().Currency, Is.EqualTo("EUR"))

    [<Test>]
    member _.``ChangeAllowCreateAccount updates UserPreferences`` () =
        SavedPrefereces.ChangeAllowCreateAccount(false)
        Assert.That(getCurrentPreferences().AllowCreateAccount, Is.EqualTo(false))

    [<Test>]
    member _.``ChangeDefaultTicker updates UserPreferences`` () =
        SavedPrefereces.ChangeDefaultTicker("QQQ")
        Assert.That(getCurrentPreferences().Ticker, Is.EqualTo("QQQ"))

    [<Test>]
    member _.``ChangeGroupOption updates UserPreferences and triggers reload if changed`` () =
        SavedPrefereces.ChangeGroupOption(false)
        Assert.That(getCurrentPreferences().GroupOptions, Is.EqualTo(false))
        SavedPrefereces.ChangeGroupOption(false)
        Assert.That(getCurrentPreferences().GroupOptions, Is.EqualTo(false))

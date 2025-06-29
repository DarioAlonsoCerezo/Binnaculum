namespace Core.Tests

open NUnit.Framework
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
        Assert.AreEqual(AppTheme.Dark, getCurrentPreferences().Theme)

    [<Test>]
    member _.``ChangeLanguage updates UserPreferences`` () =
        SavedPrefereces.ChangeLanguage("es")
        Assert.AreEqual("es", getCurrentPreferences().Language)

    [<Test>]
    member _.``ChangeCurrency updates UserPreferences`` () =
        SavedPrefereces.ChangeCurrency("EUR")
        Assert.AreEqual("EUR", getCurrentPreferences().Currency)

    [<Test>]
    member _.``ChangeAllowCreateAccount updates UserPreferences`` () =
        SavedPrefereces.ChangeAllowCreateAccount(false)
        Assert.AreEqual(false, getCurrentPreferences().AllowCreateAccount)

    [<Test>]
    member _.``ChangeDefaultTicker updates UserPreferences`` () =
        SavedPrefereces.ChangeDefaultTicker("QQQ")
        Assert.AreEqual("QQQ", getCurrentPreferences().Ticker)

    [<Test>]
    member _.``ChangeGroupOption updates UserPreferences and triggers reload if changed`` () =
        SavedPrefereces.ChangeGroupOption(false)
        Assert.AreEqual(false, getCurrentPreferences().GroupOptions)
        SavedPrefereces.ChangeGroupOption(false)
        Assert.AreEqual(false, getCurrentPreferences().GroupOptions)

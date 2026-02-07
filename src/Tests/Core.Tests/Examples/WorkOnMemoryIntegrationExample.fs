namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System.Threading.Tasks
open Binnaculum.Core.UI

/// <summary>
/// Example integration tests demonstrating the complete WorkOnMemory() workflow.
/// These tests showcase the patterns described in the issue for using in-memory
/// mode for comprehensive integration testing without MAUI platform dependencies.
/// </summary>
[<TestClass>]
type WorkOnMemoryIntegrationExample() =

    /// <summary>
    /// Example Pattern 1: Basic in-memory preferences test.
    /// This demonstrates the minimal setup for an integration test using WorkOnMemory().
    /// Note: InitDatabase() and LoadData() require platform-specific file system access,
    /// so this example focuses on preference management which works in headless environments.
    /// </summary>
    [<TestMethod>]
    member _.``Pattern 1 - Basic in-memory preferences configuration``() =
        task {
            // Configure in-memory mode FIRST (before any operations)
            Overview.WorkOnMemory()

            // Set some preferences
            SavedPrefereces.ChangeLanguage("en")
            SavedPrefereces.ChangeCurrency("USD")

            // Verify preferences work in in-memory mode
            let prefs = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual("en", prefs.Language, "Language should be English")
            Assert.AreEqual("USD", prefs.Currency, "Currency should be USD")
        }

    /// <summary>
    /// Example Pattern 2: Test with multiple preference types.
    /// This demonstrates how to set and verify different types of preferences in in-memory mode.
    /// </summary>
    [<TestMethod>]
    member _.``Pattern 2 - Multiple preference types configuration``() =
        task {
            // Configure in-memory mode FIRST
            Overview.WorkOnMemory()

            // Configure various preference types
            SavedPrefereces.ChangeLanguage("es")
            SavedPrefereces.ChangeCurrency("EUR")
            SavedPrefereces.ChangeDefaultTicker("AAPL")
            SavedPrefereces.ChangeAllowCreateAccount(false)
            SavedPrefereces.ChangeGroupOption(true)

            // Verify all preferences were applied
            let prefs = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual("es", prefs.Language, "Language should be Spanish")
            Assert.AreEqual("EUR", prefs.Currency, "Currency should be EUR")
            Assert.AreEqual("AAPL", prefs.Ticker, "Ticker should be AAPL")
            Assert.IsFalse(prefs.AllowCreateAccount, "Create account should be disabled")
            Assert.IsTrue(prefs.GroupOptions, "Group options should be enabled")
        }

    /// <summary>
    /// Example Pattern 3a: Preference configuration with English and USD.
    /// Demonstrates in-memory preference management for Scenario 1.
    /// NOTE: Moved to separate test to avoid mid-test database reset issues.
    /// </summary>
    [<TestMethod>]
    member _.``Pattern 3a - Preference configuration scenario 1 (English USD)``() =
        task {
            // Configure in-memory mode
            Overview.WorkOnMemory()

            // Scenario 1: English with USD
            SavedPrefereces.ChangeLanguage("en")
            SavedPrefereces.ChangeCurrency("USD")
            let prefs1 = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual("en", prefs1.Language, "Scenario 1: Language should be English")
            Assert.AreEqual("USD", prefs1.Currency, "Scenario 1: Currency should be USD")
        }

    /// <summary>
    /// Example Pattern 3b: Preference configuration with Spanish and EUR.
    /// Demonstrates in-memory preference management for Scenario 2.
    /// Each scenario runs in its own fresh test context (NUnit handles setup/teardown).
    /// NOTE: Moved to separate test to avoid mid-test database reset issues.
    /// </summary>
    [<TestMethod>]
    member _.``Pattern 3b - Preference configuration scenario 2 (Spanish EUR)``() =
        task {
            // Configure in-memory mode (fresh start for this test)
            Overview.WorkOnMemory()

            // Scenario 2: Spanish with EUR (fresh state)
            SavedPrefereces.ChangeLanguage("es")
            SavedPrefereces.ChangeCurrency("EUR")
            let prefs2 = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual("es", prefs2.Language, "Scenario 2: Language should be Spanish")
            Assert.AreEqual("EUR", prefs2.Currency, "Scenario 2: Currency should be EUR")
        }

    /// <summary>
    /// Example Pattern 4: Secure storage (API keys) testing.
    /// This demonstrates how to test secure storage functionality in in-memory mode.
    /// </summary>
    [<TestMethod>]
    member _.``Pattern 4 - Secure storage API key persistence``() =
        task {
            // Configure in-memory mode
            Overview.WorkOnMemory()

            // Set a test API key
            let testKey = "test-polygon-api-key-xyz123"
            do! SavedPrefereces.ChangePolygonApiKey(Some testKey)

            // Verify the API key was stored
            let prefs = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual(Some testKey, prefs.PolygonApiKey, "API key should be stored in memory")

            // Verify we can load the key asynchronously
            do! SavedPrefereces.LoadPolygonApiKeyAsync()
            let prefsAfterLoad = SavedPrefereces.UserPreferences.Value

            Assert.AreEqual(Some testKey, prefsAfterLoad.PolygonApiKey, "API key should persist after async load")

            // Verify we can remove the key
            do! SavedPrefereces.ChangePolygonApiKey(None)
            let prefsAfterRemove = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual(None, prefsAfterRemove.PolygonApiKey, "API key should be removed")
        }

    /// <summary>
    /// Example Pattern 5: Reactive verification with preferences.
    /// This demonstrates how to verify that preference changes properly update
    /// the reactive UserPreferences BehaviorSubject.
    /// </summary>
    [<TestMethod>]
    member _.``Pattern 5 - Preference changes update reactive subscribers``() =
        task {
            // Configure in-memory mode
            Overview.WorkOnMemory()

            // Track preference updates
            let mutable languageUpdated = ""
            let mutable currencyUpdated = ""
            let mutable updateCount = 0

            // Subscribe to preference changes
            use subscription =
                SavedPrefereces.UserPreferences.Subscribe(fun prefs ->
                    languageUpdated <- prefs.Language
                    currencyUpdated <- prefs.Currency
                    updateCount <- updateCount + 1)

            // Initial update happens on subscription
            let initialCount = updateCount

            // Change language
            SavedPrefereces.ChangeLanguage("fr")
            Assert.AreEqual("fr", languageUpdated, "Language should be updated to French")
            Assert.IsTrue(updateCount > initialCount, "Update count should increase")

            // Change currency
            SavedPrefereces.ChangeCurrency("CHF")
            Assert.AreEqual("CHF", currencyUpdated, "Currency should be updated to CHF")
        }

    /// <summary>
    /// Comprehensive preference management test demonstrating the complete workflow:
    /// 1. Configure in-memory mode
    /// 2. Set all preference types
    /// 3. Verify state
    /// 4. Clear preferences and repeat with different settings
    ///
    /// This showcases the full power of WorkOnMemory() for preference management
    /// testing without platform dependencies. For database integration tests,
    /// use this pattern in environments with MAUI platform services available
    /// (such as Core.Platform.Tests or UI integration tests).
    /// </summary>
    [<TestMethod>]
    member _.``Comprehensive workflow - Complete preference management``() =
        task {
            // === Phase 1: Initial Setup with English/USD ===
            Overview.WorkOnMemory()

            // Configure initial preferences
            SavedPrefereces.ChangeLanguage("en")
            SavedPrefereces.ChangeCurrency("USD")
            SavedPrefereces.ChangeAllowCreateAccount(true)
            SavedPrefereces.ChangeGroupOption(true)
            SavedPrefereces.ChangeDefaultTicker("SPY")

            // Set API key
            do! SavedPrefereces.ChangePolygonApiKey(Some "key-phase1")

            // Verify Phase 1 state
            let prefs1 = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual("en", prefs1.Language, "Phase 1: Language should be English")
            Assert.AreEqual("USD", prefs1.Currency, "Phase 1: Currency should be USD")
            Assert.IsTrue(prefs1.AllowCreateAccount, "Phase 1: Create account enabled")
            Assert.IsTrue(prefs1.GroupOptions, "Phase 1: Group options enabled")
            Assert.AreEqual("SPY", prefs1.Ticker, "Phase 1: Ticker should be SPY")
            Assert.AreEqual(Some "key-phase1", prefs1.PolygonApiKey, "Phase 1: API key set")

            // === Phase 2: Clear preferences and Configure with Spanish/EUR ===
            // Manually clear preferences (WipeAllDataForTesting requires database initialization)
            SavedPrefereces.clearInMemoryPreferences ()
            Overview.WorkOnMemory() // Reinitialize for fresh state

            // Configure different preferences
            SavedPrefereces.ChangeLanguage("es")
            SavedPrefereces.ChangeCurrency("EUR")
            SavedPrefereces.ChangeAllowCreateAccount(false)
            SavedPrefereces.ChangeGroupOption(false)
            SavedPrefereces.ChangeDefaultTicker("AAPL")

            // Set different API key
            do! SavedPrefereces.ChangePolygonApiKey(Some "key-phase2")

            // Verify Phase 2 state (should be completely independent from Phase 1)
            let prefs2 = SavedPrefereces.UserPreferences.Value
            Assert.AreEqual("es", prefs2.Language, "Phase 2: Language should be Spanish")
            Assert.AreEqual("EUR", prefs2.Currency, "Phase 2: Currency should be EUR")
            Assert.IsFalse(prefs2.AllowCreateAccount, "Phase 2: Create account disabled")
            Assert.IsFalse(prefs2.GroupOptions, "Phase 2: Group options disabled")
            Assert.AreEqual("AAPL", prefs2.Ticker, "Phase 2: Ticker should be AAPL")
            Assert.AreEqual(Some "key-phase2", prefs2.PolygonApiKey, "Phase 2: Different API key")
        }

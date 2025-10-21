namespace Core.Tests

open NUnit.Framework
open System.Threading.Tasks
open Binnaculum.Core.UI

/// <summary>
/// Example integration tests demonstrating the complete WorkOnMemory() workflow.
/// These tests showcase the patterns described in the issue for using in-memory
/// mode for comprehensive integration testing without MAUI platform dependencies.
/// </summary>
[<TestFixture>]
type WorkOnMemoryIntegrationExample() =

    /// <summary>
    /// Example Pattern 1: Basic in-memory preferences test.
    /// This demonstrates the minimal setup for an integration test using WorkOnMemory().
    /// Note: InitDatabase() and LoadData() require platform-specific file system access,
    /// so this example focuses on preference management which works in headless environments.
    /// </summary>
    [<Test>]
    member _.``Pattern 1 - Basic in-memory preferences configuration`` () = task {
        // Configure in-memory mode FIRST (before any operations)
        Overview.WorkOnMemory()
        
        // Set some preferences
        SavedPrefereces.ChangeLanguage("en")
        SavedPrefereces.ChangeCurrency("USD")
        
        // Verify preferences work in in-memory mode
        let prefs = SavedPrefereces.UserPreferences.Value
        Assert.That(prefs.Language, Is.EqualTo("en"), "Language should be English")
        Assert.That(prefs.Currency, Is.EqualTo("USD"), "Currency should be USD")
    }

    /// <summary>
    /// Example Pattern 2: Test with multiple preference types.
    /// This demonstrates how to set and verify different types of preferences in in-memory mode.
    /// </summary>
    [<Test>]
    member _.``Pattern 2 - Multiple preference types configuration`` () = task {
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
        Assert.That(prefs.Language, Is.EqualTo("es"), "Language should be Spanish")
        Assert.That(prefs.Currency, Is.EqualTo("EUR"), "Currency should be EUR")
        Assert.That(prefs.Ticker, Is.EqualTo("AAPL"), "Ticker should be AAPL")
        Assert.That(prefs.AllowCreateAccount, Is.False, "Create account should be disabled")
        Assert.That(prefs.GroupOptions, Is.True, "Group options should be enabled")
    }

    /// <summary>
    /// Example Pattern 3: Multiple scenarios with preference reset.
    /// This demonstrates how to use WipeAllDataForTesting() to reset preferences
    /// while maintaining in-memory mode for multiple test scenarios.
    /// </summary>
    [<Test>]
    member _.``Pattern 3 - Multiple scenarios with preference reset`` () = task {
        // Initial setup
        Overview.WorkOnMemory()
        
        // Scenario 1: English with USD
        SavedPrefereces.ChangeLanguage("en")
        SavedPrefereces.ChangeCurrency("USD")
        let prefs1 = SavedPrefereces.UserPreferences.Value
        Assert.That(prefs1.Language, Is.EqualTo("en"), "Scenario 1: Language should be English")
        Assert.That(prefs1.Currency, Is.EqualTo("USD"), "Scenario 1: Currency should be USD")
        
        // Reset for scenario 2 (clears preferences)
        do! Overview.WipeAllDataForTesting()
        
        // Re-configure in-memory mode for scenario 2
        Overview.WorkOnMemory()
        
        // Scenario 2: Spanish with EUR (fresh state)
        SavedPrefereces.ChangeLanguage("es")
        SavedPrefereces.ChangeCurrency("EUR")
        let prefs2 = SavedPrefereces.UserPreferences.Value
        Assert.That(prefs2.Language, Is.EqualTo("es"), "Scenario 2: Language should be Spanish")
        Assert.That(prefs2.Currency, Is.EqualTo("EUR"), "Scenario 2: Currency should be EUR")
    }

    /// <summary>
    /// Example Pattern 4: Secure storage (API keys) testing.
    /// This demonstrates how to test secure storage functionality in in-memory mode.
    /// </summary>
    [<Test>]
    member _.``Pattern 4 - Secure storage API key persistence`` () = task {
        // Configure in-memory mode
        Overview.WorkOnMemory()
        
        // Set a test API key
        let testKey = "test-polygon-api-key-xyz123"
        do! SavedPrefereces.ChangePolygonApiKey(Some testKey)
        
        // Verify the API key was stored
        let prefs = SavedPrefereces.UserPreferences.Value
        Assert.That(prefs.PolygonApiKey, Is.EqualTo(Some testKey), 
                    "API key should be stored in memory")
        
        // Verify we can load the key asynchronously
        do! SavedPrefereces.LoadPolygonApiKeyAsync()
        let prefsAfterLoad = SavedPrefereces.UserPreferences.Value
        Assert.That(prefsAfterLoad.PolygonApiKey, Is.EqualTo(Some testKey), 
                    "API key should persist after async load")
        
        // Verify we can remove the key
        do! SavedPrefereces.ChangePolygonApiKey(None)
        let prefsAfterRemove = SavedPrefereces.UserPreferences.Value
        Assert.That(prefsAfterRemove.PolygonApiKey, Is.EqualTo(None), 
                    "API key should be removed")
    }

    /// <summary>
    /// Example Pattern 5: Reactive verification with preferences.
    /// This demonstrates how to verify that preference changes properly update
    /// the reactive UserPreferences BehaviorSubject.
    /// </summary>
    [<Test>]
    member _.``Pattern 5 - Preference changes update reactive subscribers`` () = task {
        // Configure in-memory mode
        Overview.WorkOnMemory()
        
        // Track preference updates
        let mutable languageUpdated = ""
        let mutable currencyUpdated = ""
        let mutable updateCount = 0
        
        // Subscribe to preference changes
        use subscription = SavedPrefereces.UserPreferences.Subscribe(fun prefs ->
            languageUpdated <- prefs.Language
            currencyUpdated <- prefs.Currency
            updateCount <- updateCount + 1
        )
        
        // Initial update happens on subscription
        let initialCount = updateCount
        
        // Change language
        SavedPrefereces.ChangeLanguage("fr")
        Assert.That(languageUpdated, Is.EqualTo("fr"), "Language should be updated to French")
        Assert.That(updateCount, Is.GreaterThan(initialCount), "Update count should increase")
        
        // Change currency
        SavedPrefereces.ChangeCurrency("CHF")
        Assert.That(currencyUpdated, Is.EqualTo("CHF"), "Currency should be updated to CHF")
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
    [<Test>]
    member _.``Comprehensive workflow - Complete preference management`` () = task {
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
        Assert.That(prefs1.Language, Is.EqualTo("en"), "Phase 1: Language should be English")
        Assert.That(prefs1.Currency, Is.EqualTo("USD"), "Phase 1: Currency should be USD")
        Assert.That(prefs1.AllowCreateAccount, Is.True, "Phase 1: Create account enabled")
        Assert.That(prefs1.GroupOptions, Is.True, "Phase 1: Group options enabled")
        Assert.That(prefs1.Ticker, Is.EqualTo("SPY"), "Phase 1: Ticker should be SPY")
        Assert.That(prefs1.PolygonApiKey, Is.EqualTo(Some "key-phase1"), "Phase 1: API key set")
        
        // === Phase 2: Clear preferences and Configure with Spanish/EUR ===
        // Manually clear preferences (WipeAllDataForTesting requires database initialization)
        SavedPrefereces.clearInMemoryPreferences()
        Overview.WorkOnMemory()  // Reinitialize for fresh state
        
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
        Assert.That(prefs2.Language, Is.EqualTo("es"), "Phase 2: Language should be Spanish")
        Assert.That(prefs2.Currency, Is.EqualTo("EUR"), "Phase 2: Currency should be EUR")
        Assert.That(prefs2.AllowCreateAccount, Is.False, "Phase 2: Create account disabled")
        Assert.That(prefs2.GroupOptions, Is.False, "Phase 2: Group options disabled")
        Assert.That(prefs2.Ticker, Is.EqualTo("AAPL"), "Phase 2: Ticker should be AAPL")
        Assert.That(prefs2.PolygonApiKey, Is.EqualTo(Some "key-phase2"), "Phase 2: Different API key")
    }

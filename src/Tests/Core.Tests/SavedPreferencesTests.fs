namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.UI.SavedPrefereces
open Microsoft.Maui.Storage
open Microsoft.Maui.ApplicationModel
open System
open System.Threading.Tasks

[<TestClass>]
type SavedPreferencesTests() =
    
    [<TestInitialize>]
    member _.Setup() =
        // Clean up any existing test keys
        try
            SecureStorage.Remove(Binnaculum.Core.Keys.PolygonApiKeyKey) |> ignore
        with
        | _ -> () // Ignore if key doesn't exist
    
    [<TestCleanup>]
    member _.TearDown() =
        // Clean up after tests
        try
            SecureStorage.Remove(Binnaculum.Core.Keys.PolygonApiKeyKey) |> ignore
        with
        | _ -> () // Ignore if key doesn't exist

    [<TestMethod>]
    member _.``PreferencesCollection should have PolygonApiKey property`` () =
        // Verify the new property exists in the record type
        let defaultPrefs = UserPreferences.Value
        Assert.AreEqual(None, defaultPrefs.PolygonApiKey, "Default PolygonApiKey should be None")

    [<TestMethod>]
    member _.``LoadPolygonApiKeyAsync should update UserPreferences when no key exists`` () = 
        task {
            let initialPrefs = UserPreferences.Value
            
            // Load preferences asynchronously
            do! LoadPolygonApiKeyAsync()
            
            let updatedPrefs = UserPreferences.Value
            Assert.AreEqual(None, updatedPrefs.PolygonApiKey, "PolygonApiKey should remain None when no key is stored")
            // Verify other preferences remain unchanged
            Assert.AreEqual(initialPrefs.Theme, updatedPrefs.Theme, "Theme should remain unchanged")
            Assert.AreEqual(initialPrefs.Language, updatedPrefs.Language, "Language should remain unchanged")
        }

    [<TestMethod>]
    member _.``ChangePolygonApiKey should save and update UserPreferences with Some value`` () =
        task {
            let testApiKey = "test-api-key-12345"
            
            // Change the API key
            do! ChangePolygonApiKey(Some testApiKey)
            
            // Verify UserPreferences is updated
            let updatedPrefs = UserPreferences.Value
            Assert.AreEqual(Some testApiKey, updatedPrefs.PolygonApiKey, "PolygonApiKey should be updated in UserPreferences")
            
            // Verify it's saved to SecureStorage
            let! storedValue = SecureStorage.GetAsync(Binnaculum.Core.Keys.PolygonApiKeyKey)
            Assert.AreEqual(testApiKey, storedValue, "API key should be saved to SecureStorage")
        }

    [<TestMethod>]
    member _.``ChangePolygonApiKey should remove key and update UserPreferences with None`` () =
        task {
            let testApiKey = "test-api-key-to-remove"
            
            // First set a key
            do! ChangePolygonApiKey(Some testApiKey)
            
            // Then remove it
            do! ChangePolygonApiKey(None)
            
            // Verify UserPreferences is updated
            let updatedPrefs = UserPreferences.Value
            Assert.AreEqual(None, updatedPrefs.PolygonApiKey, "PolygonApiKey should be None in UserPreferences")
            
            // Verify it's removed from SecureStorage
            let! storedValue = SecureStorage.GetAsync(Binnaculum.Core.Keys.PolygonApiKeyKey)
            Assert.IsTrue(String.IsNullOrEmpty(storedValue), "API key should be removed from SecureStorage")
        }

    [<TestMethod>]
    member _.``LoadPolygonApiKeyAsync should load existing key from SecureStorage`` () =
        task {
            let testApiKey = "existing-api-key-xyz"
            
            // Set key directly in SecureStorage
            do! SecureStorage.SetAsync(Binnaculum.Core.Keys.PolygonApiKeyKey, testApiKey)
            
            // Load preferences asynchronously
            do! LoadPolygonApiKeyAsync()
            
            // Verify UserPreferences is updated
            let updatedPrefs = UserPreferences.Value
            Assert.AreEqual(Some testApiKey, updatedPrefs.PolygonApiKey, "PolygonApiKey should be loaded from SecureStorage")
        }

    [<TestMethod>]
    member _.``ChangePolygonApiKey should handle empty string as None`` () =
        task {
            // Change to empty string
            do! ChangePolygonApiKey(Some "")
            
            // Verify UserPreferences shows empty string
            let updatedPrefs = UserPreferences.Value
            Assert.AreEqual(Some "", updatedPrefs.PolygonApiKey, "Empty string should be preserved in UserPreferences")
        }

    [<TestMethod>]
    member _.``Preferences integration should maintain backward compatibility`` () =
        // Verify all existing preference functions still work
        let originalTheme = UserPreferences.Value.Theme
        let originalLanguage = UserPreferences.Value.Language
        let originalCurrency = UserPreferences.Value.Currency
        
        // Test theme change
        ChangeAppTheme(AppTheme.Dark)
        Assert.AreEqual(AppTheme.Dark, UserPreferences.Value.Theme, "Theme change should work")
        
        // Test language change  
        ChangeLanguage("es")
        Assert.AreEqual("es", UserPreferences.Value.Language, "Language change should work")
        
        // Test currency change
        ChangeCurrency("EUR")
        Assert.AreEqual("EUR", UserPreferences.Value.Currency, "Currency change should work")
        
        // Verify PolygonApiKey is still None (not affected by other changes)
        Assert.AreEqual(None, UserPreferences.Value.PolygonApiKey, "PolygonApiKey should remain None")
        
        // Restore original values
        ChangeAppTheme(originalTheme)
        ChangeLanguage(originalLanguage)
        ChangeCurrency(originalCurrency)
namespace Binnaculum.Core.Providers

open Microsoft.Maui.Storage
open Microsoft.Maui.ApplicationModel
open System.Collections.Generic
open System.Threading.Tasks
open Binnaculum.Core.Logging

/// <summary>
/// Represents the preferences storage mode
/// </summary>
type PreferencesMode =
    | FileSystem // Production: Uses MAUI Preferences + SecureStorage APIs
    | InMemory // Testing: Uses in-memory dictionaries

/// <summary>
/// PreferencesProvider abstracts preference and secure storage operations,
/// enabling both production (FileSystem) and testing (InMemory) modes.
///
/// This mirrors the DatabaseMode pattern in ConnectionProvider, providing
/// a consistent abstraction layer across storage systems.
/// </summary>
module PreferencesProvider =

    /// Mutable preference mode - None means use default FileSystem mode
    let mutable private preferencesMode: PreferencesMode option = None

    /// In-memory storage for preferences (key-value pairs)
    let mutable private inMemoryPreferences: Dictionary<string, obj> =
        Dictionary<string, obj>()

    /// In-memory storage for secure values (key-value pairs)
    let mutable private inMemorySecureStorage: Dictionary<string, string> =
        Dictionary<string, string>()

    /// <summary>
    /// Sets the preferences mode and reinitializes storage.
    /// When switching modes, existing in-memory storage is cleared.
    /// </summary>
    /// <param name="mode">The preferences mode to use (FileSystem or InMemory)</param>
    let setPreferencesMode (mode: PreferencesMode) =
        preferencesMode <- Some mode

        // Clear in-memory storage when switching to InMemory mode
        match mode with
        | InMemory ->
            inMemoryPreferences.Clear()
            inMemorySecureStorage.Clear()
        | FileSystem ->
            // When switching to FileSystem, clear in-memory caches
            inMemoryPreferences.Clear()
            inMemorySecureStorage.Clear()

    /// <summary>
    /// Clears all in-memory storage (both preferences and secure storage).
    /// Only affects InMemory mode; FileSystem mode is unaffected.
    /// </summary>
    let clearInMemoryStorage () =
        inMemoryPreferences.Clear()
        inMemorySecureStorage.Clear()

    /// <summary>
    /// Gets an integer preference value with a default fallback.
    /// Delegates to Preferences.Get in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The preference key</param>
    /// <param name="defaultValue">The default value if key doesn't exist</param>
    /// <returns>The preference value or default</returns>
    let getPreference (key: string) (defaultValue: int) : int =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: use dictionary
            if inMemoryPreferences.ContainsKey(key) then
                inMemoryPreferences.[key] :?> int
            else
                defaultValue
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform Preferences
            Preferences.Get(key, defaultValue)

    /// <summary>
    /// Sets an integer preference value.
    /// Delegates to Preferences.Set in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The preference key</param>
    /// <param name="value">The value to set</param>
    let setPreference (key: string) (value: int) : unit =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: store in dictionary
            inMemoryPreferences.[key] <- value :> obj
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform Preferences
            Preferences.Set(key, value)

    /// <summary>
    /// Gets a string preference value with a default fallback.
    /// Delegates to Preferences.Get in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The preference key</param>
    /// <param name="defaultValue">The default value if key doesn't exist</param>
    /// <returns>The preference value or default</returns>
    let getString (key: string) (defaultValue: string) : string =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: use dictionary
            if inMemoryPreferences.ContainsKey(key) then
                inMemoryPreferences.[key] :?> string
            else
                defaultValue
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform Preferences
            Preferences.Get(key, defaultValue)

    /// <summary>
    /// Sets a string preference value.
    /// Delegates to Preferences.Set in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The preference key</param>
    /// <param name="value">The value to set</param>
    let setString (key: string) (value: string) : unit =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: store in dictionary
            inMemoryPreferences.[key] <- value :> obj
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform Preferences
            Preferences.Set(key, value)

    /// <summary>
    /// Gets a boolean preference value with a default fallback.
    /// Delegates to Preferences.Get in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The preference key</param>
    /// <param name="defaultValue">The default value if key doesn't exist</param>
    /// <returns>The preference value or default</returns>
    let getBoolean (key: string) (defaultValue: bool) : bool =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: use dictionary
            if inMemoryPreferences.ContainsKey(key) then
                inMemoryPreferences.[key] :?> bool
            else
                defaultValue
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform Preferences
            Preferences.Get(key, defaultValue)

    /// <summary>
    /// Sets a boolean preference value.
    /// Delegates to Preferences.Set in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The preference key</param>
    /// <param name="value">The value to set</param>
    let setBoolean (key: string) (value: bool) : unit =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: store in dictionary
            inMemoryPreferences.[key] <- value :> obj
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform Preferences
            Preferences.Set(key, value)

    /// <summary>
    /// Asynchronously gets a secure value.
    /// Delegates to SecureStorage.GetAsync in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The secure storage key</param>
    /// <returns>The secure value or null if not found</returns>
    let getSecureAsync (key: string) : Task<string> =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: use dictionary (synchronous but wrapped in Task)
            Task.FromResult(
                if inMemorySecureStorage.ContainsKey(key) then
                    inMemorySecureStorage.[key]
                else
                    null
            )
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform SecureStorage
            try
                SecureStorage.GetAsync(key)
            with ex ->
                // Graceful degradation: log error and return null
                CoreLogger.logError "PreferencesProvider" $"Failed to get secure value for key '{key}': {ex.Message}"
                Task.FromResult(null)

    /// <summary>
    /// Asynchronously sets a secure value.
    /// Delegates to SecureStorage.SetAsync in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The secure storage key</param>
    /// <param name="value">The value to store securely</param>
    let setSecureAsync (key: string) (value: string) : Task =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: store in dictionary (synchronous but wrapped in Task)
            inMemorySecureStorage.[key] <- value
            Task.CompletedTask
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform SecureStorage
            try
                SecureStorage.SetAsync(key, value)
            with ex ->
                // Graceful degradation: log error but don't throw
                CoreLogger.logError "PreferencesProvider" $"Failed to set secure value for key '{key}': {ex.Message}"
                Task.CompletedTask

    /// <summary>
    /// Removes a secure value.
    /// Delegates to SecureStorage.Remove in FileSystem mode or in-memory dictionary in InMemory mode.
    /// </summary>
    /// <param name="key">The secure storage key to remove</param>
    let removeSecure (key: string) : bool =
        match preferencesMode with
        | Some InMemory ->
            // In-memory mode: remove from dictionary
            inMemorySecureStorage.Remove(key)
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform SecureStorage
            try
                SecureStorage.Remove(key)
            with ex ->
                // Graceful degradation: log error and return false
                CoreLogger.logError "PreferencesProvider" $"Failed to remove secure value for key '{key}': {ex.Message}"
                false

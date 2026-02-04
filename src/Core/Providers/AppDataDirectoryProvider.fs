namespace Binnaculum.Core.Providers

open Microsoft.Maui.Storage
open System.IO

/// <summary>
/// Represents the AppDataDirectory provider mode
/// </summary>
type AppDataDirectoryMode =
    | FileSystem // Production: Uses MAUI FileSystem.AppDataDirectory
    | InMemory // Testing: Uses in-memory temporary directory

/// <summary>
/// AppDataDirectoryProvider abstracts file system directory operations,
/// enabling both production (FileSystem) and testing (InMemory) modes.
///
/// This mirrors the DatabaseMode pattern in ConnectionProvider and PreferencesMode
/// in PreferencesProvider, providing a consistent abstraction layer for storage access.
///
/// Usage:
///   Production: AppDataDirectoryProvider.getAppDataDirectory() → Platform-specific directory
///   Testing:    AppDataDirectoryProvider.setMode(InMemory) → In-memory temp directory
/// </summary>
module AppDataDirectoryProvider =

    /// Mutable directory mode - None means use default FileSystem mode
    let mutable private directoryMode: AppDataDirectoryMode option = None

    /// In-memory temporary directory for testing
    let mutable private inMemoryTempDirectory: string option = None

    /// <summary>
    /// Sets the AppDataDirectory provider mode and reinitializes storage.
    /// When switching to InMemory mode, creates a new temporary directory.
    /// </summary>
    /// <param name="mode">The directory mode to use (FileSystem or InMemory)</param>
    let setMode (mode: AppDataDirectoryMode) =
        directoryMode <- Some mode

        match mode with
        | InMemory ->
            // Create a new temporary directory for in-memory testing
            let guid = System.Guid.NewGuid().ToString("N")[0..7]
            let tempPath = Path.Combine(Path.GetTempPath(), $"BinnaculumTest_{guid}")

            if not (Directory.Exists(tempPath)) then
                Directory.CreateDirectory(tempPath) |> ignore

            inMemoryTempDirectory <- Some tempPath
        | FileSystem ->
            // Clean up any in-memory directory when switching to FileSystem
            match inMemoryTempDirectory with
            | Some path when Directory.Exists(path) -> Directory.Delete(path, true)
            | _ -> ()

            inMemoryTempDirectory <- None

    /// <summary>
    /// Clears the in-memory temporary directory (deletes all files and folder).
    /// Only affects InMemory mode; FileSystem mode is unaffected.
    /// </summary>
    let clearInMemoryDirectory () =
        match inMemoryTempDirectory with
        | Some path when Directory.Exists(path) ->
            Directory.Delete(path, true)
            // Recreate an empty directory
            Directory.CreateDirectory(path) |> ignore
        | _ -> ()

    /// <summary>
    /// Gets the AppDataDirectory path.
    /// In FileSystem mode: Uses MAUI FileSystem.AppDataDirectory (platform-specific)
    /// In InMemory mode: Uses a temporary directory for testing
    /// </summary>
    /// <returns>The absolute path to the AppDataDirectory</returns>
    let getAppDataDirectory () : string =
        match directoryMode with
        | Some InMemory ->
            // In-memory mode: return the temporary directory
            match inMemoryTempDirectory with
            | Some path -> path
            | None ->
                // Initialize if not already done
                let guid = System.Guid.NewGuid().ToString("N")[0..7]
                let tempPath = Path.Combine(Path.GetTempPath(), $"BinnaculumTest_{guid}")
                Directory.CreateDirectory(tempPath) |> ignore
                inMemoryTempDirectory <- Some tempPath
                tempPath
        | Some FileSystem
        | None ->
            // FileSystem mode (or default): use platform FileSystem.AppDataDirectory
            FileSystem.AppDataDirectory

    /// <summary>
    /// Gets the current mode (useful for verification in tests)
    /// </summary>
    /// <returns>The current AppDataDirectory mode</returns>
    let getMode () : AppDataDirectoryMode =
        match directoryMode with
        | Some mode -> mode
        | None -> FileSystem

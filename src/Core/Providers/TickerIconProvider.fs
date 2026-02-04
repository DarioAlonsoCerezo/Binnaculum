namespace Binnaculum.Core.Providers

open System
open System.IO
open System.Net.Http
open System.Threading.Tasks

/// <summary>
/// TickerIconProvider manages ticker icon files from both local cache and remote repository.
///
/// Flow:
/// 1. Check local ticker_icons folder for {symbol}.png
/// 2. If not found, attempt download from GitHub repository
/// 3. Cache downloaded icon locally and return path
/// 4. Return None on any failures (non-blocking, silent)
/// </summary>
module TickerIconProvider =

    let private iconFolderName = "ticker_icons"

    let private githubBaseUrl =
        "https://raw.githubusercontent.com/DarioAlonsoCerezo/icons/main/ticker_icons"

    let private httpClient = lazy (new HttpClient())

    /// <summary>
    /// Gets or creates the ticker_icons directory in app data directory
    /// </summary>
    let private getTickerIconsDirectory () : string =
        let appDataDir = AppDataDirectoryProvider.getAppDataDirectory ()
        let iconDir = Path.Combine(appDataDir, iconFolderName)

        if not (Directory.Exists(iconDir)) then
            Directory.CreateDirectory(iconDir) |> ignore

        iconDir

    /// <summary>
    /// Checks if icon exists locally
    /// </summary>
    let private getLocalIconPath (symbol: string) : string option =
        let iconDir = getTickerIconsDirectory ()
        let iconPath = Path.Combine(iconDir, $"{symbol}.png")
        if File.Exists(iconPath) then Some iconPath else None

    /// <summary>
    /// Downloads icon from GitHub repository
    /// </summary>
    let private downloadIcon (symbol: string) : Task<byte[] option> =
        task {
            let url = $"{githubBaseUrl}/{symbol}.png"
            let! response = httpClient.Force().GetAsync(url)

            if response.IsSuccessStatusCode then
                let! content = response.Content.ReadAsByteArrayAsync()
                return Some content
            else
                return None
        }

    /// <summary>
    /// Saves downloaded icon to local cache
    /// </summary>
    let private saveIcon (symbol: string) (iconData: byte[]) : string option =
        let iconDir = getTickerIconsDirectory ()
        let iconPath = Path.Combine(iconDir, $"{symbol}.png")
        File.WriteAllBytes(iconPath, iconData)
        Some iconPath

    /// <summary>
    /// Gets ticker icon from local cache or downloads from repository
    /// Returns path to local icon file or None if not found/failed to download
    /// </summary>
    let getTickerIcon (symbol: string) : Task<string option> =
        task {
            // Normalize symbol to uppercase for consistency
            let normalizedSymbol = symbol.ToUpperInvariant().Trim()

            // Check local cache first
            match getLocalIconPath normalizedSymbol with
            | Some path -> return Some path
            | None ->
                // Attempt to download from repository
                let! iconData = downloadIcon normalizedSymbol

                match iconData with
                | Some data -> return saveIcon normalizedSymbol data
                | None -> return None
        }
